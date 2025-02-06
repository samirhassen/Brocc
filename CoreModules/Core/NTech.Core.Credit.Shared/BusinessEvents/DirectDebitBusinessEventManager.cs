using nCredit.DomainModel;
using NTech.Banking.Autogiro;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers.Se;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel
{

    public class DirectDebitBusinessEventManager : DirectDebitOnCreditCreationBusinessEventManager
    {
        private readonly CreditContextFactory contextFactory;
        private readonly ICreditEnvSettings envSettings;

        public DirectDebitBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock coreClock, IClientConfigurationCore clientConfiguration, 
            CreditContextFactory contextFactory, ICreditEnvSettings envSettings) : base(currentUser, coreClock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.envSettings = envSettings;
        }

        private Dictionary<string, string> FindPaymentNrToCreditNrMatches(ICreditContextExtended context, AutogiroMedgivandeAviseringFileParser.File file)
        {
            var d = new Dictionary<string, string>();

            foreach (var g in file.ResultItems.Select(x => x.PaymentNr).ToArray().SplitIntoGroupsOfN(200))
            {
                var matches = context
                    .CreditOutgoingDirectDebitItemsQueryable
                    .Where(x => g.Contains(x.PaymentNr))
                    .Select(x => new { x.PaymentNr, x.CreditNr })
                    .ToList()
                    .GroupBy(x => x.PaymentNr)
                    .ToList();
                foreach (var m in matches)
                {
                    if (m.Count() == 1)
                        d[m.Key] = m.Single().CreditNr;
                }
            }

            //Can happen when someone goes through ag-online directly
            var unmatched = file.ResultItems.Where(x => !d.ContainsKey(x.PaymentNr)).ToList();
            if (unmatched.Any())
            {
                var g = new AutogiroPaymentNumberGenerator();
                foreach (var u in unmatched)
                {
                    string partialCreditNr;
                    int? applicantNr;
                    if (g.TryExtractPartialCreditNrAndApplicantNrFromPaymentNr(u.PaymentNr, out partialCreditNr, out applicantNr))
                    {
                        var nrs = context.CreditHeadersQueryable.Where(x => x.CreditNr.EndsWith(partialCreditNr)).Select(x => x.CreditNr).ToList();
                        if (nrs.Count == 1)
                        {
                            if (g.GenerateNr(nrs[0], applicantNr.Value) == u.PaymentNr)
                            {
                                d[u.PaymentNr] = nrs[0];
                            }
                        }
                    }
                }
            }

            return d;
        }

        public bool TryScheduleDirectDebitCancellation(ICreditContextExtended context, string creditNr, string paymentNr, BankGiroNumberSe clientBankGiroNr, out string failedMessage, BusinessEvent evt = null)
        {
            return TryScheduleDirectDebitOperation(context, creditNr, OutgoingDirectDebitFileOperation.Cancellation, null, paymentNr, null, clientBankGiroNr, out failedMessage, evt: evt);
        }



        public bool TryScheduleDirectDebitChange(ICreditContextExtended context, string creditNr, BankAccountNumberSe bankAccountNr, string paymentNr, int? customerId, BankGiroNumberSe clientBankGiroNr, string currentStatus, out string failedMessage, BusinessEvent evt = null)
        {
            var operation = currentStatus == OutgoingDirectDebitFileOperation.Activation.ToString() ? OutgoingDirectDebitFileOperation.Activation : OutgoingDirectDebitFileOperation.Cancellation;

            return TryScheduleDirectDebitOperation(context, creditNr, operation, bankAccountNr, paymentNr, customerId, clientBankGiroNr, out failedMessage, evt: evt);
        }

        public bool TryImportIncomingDirectDebitStatusChangeFile(ICreditContextExtended context, AutogiroMedgivandeAviseringFileParser.File file, string filename, Lazy<string> rawFileDocumentArchiveKey, out string failedMessage)
        {
            var g = new AutogiroPaymentNumberGenerator();

            var creditNrByPaymentNr = FindPaymentNrToCreditNrMatches(context, file);
            var eventType = BusinessEventType.ImportedIncomingDirectDebitChangeFile;
            var evt = AddBusinessEvent(eventType, context);

            foreach (var item in file.ResultItems)
            {
                if (creditNrByPaymentNr.ContainsKey(item.PaymentNr))
                {
                    var creditNr = creditNrByPaymentNr[item.PaymentNr];
                    if (!item.Action.HasValue)
                    {
                        AddComment($"Incoming direct debit status change ignored on payment nr {item.PaymentNr} ({item.InformationCode} {item.CommentCode}).", eventType, context, creditNr: creditNr, evt: evt, attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(rawFileDocumentArchiveKey.Value));
                    }
                    else
                    {
                        switch (item.Action.Value)
                        {
                            case AutogiroMedgivandeAviseringFileParser.AgActionCode.Start:
                                {
                                    var applicantNr = g.ExtractApplicantNrOrNull(item.PaymentNr, creditNr);
                                    string failedM;
                                    if (!TryChangeDirectDebitStatus(context, creditNr, true, item.CustomerBankAccountNr, applicantNr, out failedM, evt: evt))
                                    {
                                        AddComment($"Incoming direct debit activation of payment nr {item.PaymentNr} ({item.InformationCode} {item.CommentCode}) failed: {failedM}", eventType, context, creditNr: creditNr, evt: evt, attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(rawFileDocumentArchiveKey.Value));
                                    }
                                }
                                break;
                            case AutogiroMedgivandeAviseringFileParser.AgActionCode.Cancel:
                                {
                                    var applicantNr = g.ExtractApplicantNrOrNull(item.PaymentNr, creditNr);
                                    //NOTE: Bank account nr will not be present on a cancellation and so will be removed which is kind of iffy ... strictly correct since we no longer know this value but still
                                    string failedM;
                                    if (!TryChangeDirectDebitStatus(context, creditNr, false, item.CustomerBankAccountNr, applicantNr, out failedM, evt: evt))
                                    {
                                        AddComment($"Incoming direct debit cancellation of payment nr {item.PaymentNr} ({item.InformationCode} {item.CommentCode}) failed: {failedM}", eventType, context, creditNr: creditNr, evt: evt, attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(rawFileDocumentArchiveKey.Value));
                                    }
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                else
                {
                    //TODO: Where/how do we log these?
                }
            }

            context.AddIncomingDirectDebitStatusChangeFileHeaders(FillInInfrastructureFields(new IncomingDirectDebitStatusChangeFileHeader
            {
                FileArchiveKey = rawFileDocumentArchiveKey.Value,
                CreatedByEvent = evt,
                Filename = filename,
                TransactionDate = evt.TransactionDate
            }));

            failedMessage = null;

            return true;
        }

        public bool TryChangeDirectDebitStatus(ICreditContextExtended context, string creditNr, bool isActive, BankAccountNumberSe bankAccountNr, int? bankAccountOwnerApplicantNr, out string failedMessage, BusinessEvent evt = null)
        {
            evt = evt ?? AddBusinessEvent(BusinessEventType.ChangeDirectDebitStatus, context);

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "creditNr missing";
                return false;
            }

            if (isActive)
            {

                if (isActive && !bankAccountOwnerApplicantNr.HasValue)
                {
                    failedMessage = "Cannot set direct debit to active without an account owner applicant";
                    return false;
                }

                var credit = context.CreditHeadersQueryable.Single(x => x.CreditNr == creditNr);
                var model = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);

                if (bankAccountOwnerApplicantNr.HasValue && (bankAccountOwnerApplicantNr.Value < 1 || bankAccountOwnerApplicantNr.Value > credit.NrOfApplicants))
                {
                    failedMessage = "Invalid direct debit account applicant nr";
                    return false;
                }

                var currentIsActive = model.GetIsDirectDebitActive(Clock.Today);
                if (isActive != currentIsActive)
                    AddDatedCreditString(DatedCreditStringCode.IsDirectDebitActive.ToString(), isActive ? "true" : "false", credit, evt, context);

                var currentBankAccountOwnerApplicantNr = model.GetDirectDebitAccountOwnerApplicantNr(Clock.Today);
                if (bankAccountOwnerApplicantNr.HasValue && bankAccountOwnerApplicantNr != currentBankAccountOwnerApplicantNr)
                    AddDatedCreditString(DatedCreditStringCode.DirectDebitAccountOwnerApplicantNr.ToString(), bankAccountOwnerApplicantNr.Value.ToString(), credit, evt, context);

                var currentBankAccountNr = model.GetDirectDebitBankAccountNr(Clock.Today);
                if (bankAccountNr != null && currentBankAccountNr?.PaymentFileFormattedNr != bankAccountNr?.PaymentFileFormattedNr)
                    AddDatedCreditString(DatedCreditStringCode.DirectDebitBankAccountNr.ToString(), bankAccountNr.PaymentFileFormattedNr, credit, evt, context);

                failedMessage = null;
                return true;
            }

            else
            {
                var credit = context.CreditHeadersQueryable.Single(x => x.CreditNr == creditNr);
                var model = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);

                var currentIsActive = model.GetIsDirectDebitActive(Clock.Today);
                if (isActive != currentIsActive)
                    AddDatedCreditString(DatedCreditStringCode.IsDirectDebitActive.ToString(), isActive ? "true" : "false", credit, evt, context);

                failedMessage = null;
                return true;
            }
        }

        public enum InternalDirectDebitFileOperation
        {
            PendingActivation,
            PendingCancellation,
            PendingChange
        }


        public bool TryCreateOutgoingDirectDebitStatusFile(AutogiroSettingsModel settings, BankGiroNumberSe clientBankGiroNr, bool isTest, ICustomerClient customerClient, IDocumentClient documentClient, bool skipExport, out int? outgoingDirectDebitStatusChangeFileHeaderId, out string failedMessage, out List<Tuple<CreditOutgoingDirectDebitItem, string>> skippedItemsWithErrors)
        {
            using (var context = contextFactory.CreateContext())
            {
                var items = context
                    .CreditOutgoingDirectDebitItemsQueryable
                    .Where(x => !x.OutgoingDirectDebitStatusChangeFileHeaderId.HasValue)
                    .ToList();

                skippedItemsWithErrors = new List<Tuple<CreditOutgoingDirectDebitItem, string>>();
                var includedItems = new List<CreditOutgoingDirectDebitItem>();

                var civicRegNrByCustomerId = customerClient
                    .BulkFetchPropertiesByCustomerIdsD(new HashSet<int>(items.Where(x => x.BankAccountOwnerCustomerId.HasValue).Select(x => x.BankAccountOwnerCustomerId.Value).Distinct()), "civicRegNr")
                    .ToDictionary(x => x.Key, x => x.Value.Opt("civicRegNr"));

                //Create a file
                var f = AutogiroStatusChangeFileToBgcBuilder.New(settings.CustomerNr, clientBankGiroNr, () => Clock.Now.DateTime, isTest);

                foreach (var activation in items.Where(x => x.Operation == OutgoingDirectDebitFileOperation.Activation.ToString()))
                {
                    BankAccountNumberSe b;
                    CivicRegNumberSe c;
                    string e;
                    if (!BankAccountNumberSe.TryParse(activation.BankAccountNr, out b, out e))
                    {
                        skippedItemsWithErrors.Add(Tuple.Create(activation, $"Invalid bank account nr: {e}"));
                    }
                    else if (!activation.BankAccountOwnerCustomerId.HasValue || !civicRegNrByCustomerId.ContainsKey(activation.BankAccountOwnerCustomerId.Value) || !CivicRegNumberSe.TryParse(civicRegNrByCustomerId[activation.BankAccountOwnerCustomerId.Value], out c))
                    {
                        skippedItemsWithErrors.Add(Tuple.Create(activation, $"Missing customer id or invalid civic regnr"));
                    }
                    else
                    {
                        f.AddActivation(activation.PaymentNr, c, b);
                        includedItems.Add(activation);
                    }
                }

                foreach (var cancellation in items.Where(x => x.Operation == OutgoingDirectDebitFileOperation.Cancellation.ToString()))
                {
                    f.AddCancellation(cancellation.PaymentNr);
                    includedItems.Add(cancellation);
                }

                if (!includedItems.Any())
                {
                    outgoingDirectDebitStatusChangeFileHeaderId = null;
                    failedMessage = null;
                    return true;
                }

                //Write the file to the archive
                var fileBytes = f.ToByteArray();
                var expectedFilename = f.GetFileName();

                var archiveKey = documentClient.ArchiveStore(fileBytes, "text/plain", expectedFilename);

                var evt = AddBusinessEvent(BusinessEventType.NewOutgoingDirectDebitChangeFile, context);
                var h = FillInInfrastructureFields(new OutgoingDirectDebitStatusChangeFileHeader
                {
                    CreatedByEvent = evt,
                    FileArchiveKey = archiveKey,
                    ExternalId = Guid.NewGuid().ToString(),
                    TransactionDate = evt.TransactionDate
                });
                foreach (var i in includedItems)
                {
                    i.OutgoingDirectDebitStatusChangeFile = h;
                }

                //Export the file
                if (!skipExport && !string.IsNullOrWhiteSpace(settings.OutgoingStatusFileExportProfileName))
                {
                    var exportResult = documentClient.ExportArchiveFile(archiveKey, settings.OutgoingStatusFileExportProfileName, null);
                    if (!exportResult.IsSuccess)
                    {
                        failedMessage = "Export failed. File creation aborted";
                        outgoingDirectDebitStatusChangeFileHeaderId = null;
                        return false;
                    }
                }

                context.SaveChanges();

                failedMessage = null;
                outgoingDirectDebitStatusChangeFileHeaderId = h.Id;
                return true;
            }
        }
    }
}