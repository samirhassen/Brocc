using nCredit.Code.Services;
using nCredit.DbModel.Repository;
using Newtonsoft.Json;
using NTech;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services.Shared;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static nCredit.CreditTermsChangeItem;

namespace nCredit.DbModel.BusinessEvents
{
    public class MortgageLoansCreditTermsChangeBusinessEventManager : CreditTermsChangeCancelOnlyBusinessEventManager
    {
        public const int DefaultInterestBindMonthCount = 3;
        private readonly LegalInterestCeilingService legalInterestCeilingService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly ILoggingService loggingService;
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly CachedSettingsService cachedSettingsService;

        public MortgageLoansCreditTermsChangeBusinessEventManager(
            INTechCurrentUserMetadata currentUser,
            LegalInterestCeilingService legalInterestCeilingService,
            ICoreClock clock,
            IClientConfigurationCore clientConfiguration,
            CreditContextFactory creditContextFactory,
            ICreditEnvSettings envSettings,
            ICustomerClient customerClient,
            ILoggingService loggingService,
            INTechServiceRegistry serviceRegistry,
            INTechEmailServiceFactory emailServiceFactory,
            CachedSettingsService cachedSettingsService)
            : base(currentUser, clock, clientConfiguration, creditContextFactory, customerClient)
        {
            this.legalInterestCeilingService = legalInterestCeilingService;
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.loggingService = loggingService;
            this.serviceRegistry = serviceRegistry;
            this.emailServiceFactory = emailServiceFactory;
            this.cachedSettingsService = cachedSettingsService;
        }

        public bool TryFetchPendingTermsChangeData(int id, out PendingChangeData pendingChangeData, out string failedMessage)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var termChangeHeader = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new
                    {
                        x.Id,
                        x.CreditNr,
                        x.Credit.NrOfApplicants,
                        Items = x.Items.Select(y => new
                        {
                            y.Name,
                            y.Value,
                            y.ApplicantNr,
                            y.CreatedByEvent.TransactionDate
                        }),
                        SentDate = x.CreatedByEvent.TransactionDate,
                        ScheduledDateRaw = x.Items.Where(y => y.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.MlScheduledDate.ToString()).Select(y => y.Value).FirstOrDefault()
                    }).Single(x => x.Id == id);

                string GetItemValue(CreditTermsChangeItemCode code, bool isRequired, Action<DateTime> observeTransactionDate = null)
                {
                    var item = termChangeHeader.Items.SingleOrDefault(x => x.Name == code.ToString());
                    if (item == null && isRequired)
                        throw new Exception($"{termChangeHeader.CreditNr}:Missing required item: {code}");
                    if (item != null)
                        observeTransactionDate?.Invoke(item.TransactionDate);
                    return item?.Value;
                }

                var newFixedMonthsCount = int.Parse(GetItemValue(CreditTermsChangeItemCode.NewInterestRebindMonthCount, true));
                var newMarginInterestRatePercent = decimal.Parse(GetItemValue(CreditTermsChangeItemCode.NewMarginInterestRatePercent, true), CultureInfo.InvariantCulture);
                var newInterestBoundFrom = DateTime.ParseExact(GetItemValue(CreditTermsChangeItemCode.NewInterestBoundFromDate, true), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var newReferenceInterestRatePercent = decimal.Parse(GetItemValue(CreditTermsChangeItemCode.NewReferenceInterestRatePercent, true), CultureInfo.InvariantCulture);

                var newTerms = new MlNewChangeTerms()
                {
                    NewFixedMonthsCount = newFixedMonthsCount,
                    NewMarginInterestRatePercent = newMarginInterestRatePercent,
                    NewInterestBoundFrom = newInterestBoundFrom,
                    NewReferenceInterestRatePercent = newReferenceInterestRatePercent
                };

                if (!TryComputeMlTermsChangeData(termChangeHeader.CreditNr, newTerms, out pendingChangeData, out failedMessage))
                {
                    failedMessage = "Could not compute terms change.";
                    return false;
                }

                DateTime? signedDate = null;
                var signedAgreementDocumentArchiveKey = GetItemValue(CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey, false, x => signedDate = x);
                var unsignedAgreementDocumentArchiveKey = GetItemValue(CreditTermsChangeItemCode.UnsignedAgreementDocumentArchiveKey, false);

                if(signedAgreementDocumentArchiveKey == null)
                {
                    pendingChangeData.ActiveSignatureSessionKey = GetItemValue(CreditTermsChangeItemCode.SignatureSessionKey, false);
                }

                pendingChangeData.Id = termChangeHeader.Id;
                pendingChangeData.SentDate = termChangeHeader.SentDate;
                pendingChangeData.Signature = new PendingChangeData.SignatureItem
                {
                    UnsignedDocumentKey = unsignedAgreementDocumentArchiveKey,
                    SignedDocumentKey = signedAgreementDocumentArchiveKey,
                    SignatureDate = signedDate
                };
                pendingChangeData.ScheduledDate = Dates.ParseDateTimeExactOrNull(termChangeHeader.ScheduledDateRaw, "o");

                return true;
            }
        }

        public bool TryComputeMlTermsChangeData<T>(string creditNr, MlNewChangeTerms newChangeTerms, out T termsChangeData, out string failedMessage) where T : MlTermsChangeData, new()
        {
            if (newChangeTerms.NewFixedMonthsCount <= 0 || !newChangeTerms.NewMarginInterestRatePercent.HasValue)
            {
                failedMessage = "Missing or invalid newInterestRebindMonthCount and/or newMarginInterestRatePercent";
                termsChangeData = null;
                return false;
            }

            newChangeTerms.NewMarginInterestRatePercent = Math.Round(newChangeTerms.NewMarginInterestRatePercent.Value, 2);

            if (IsRejectedMarginInterestRate(newChangeTerms.NewMarginInterestRatePercent))
            {
                failedMessage = "Margin interest rate outside the allowed range.";
                termsChangeData = null;
                return false;
            }

            HistoricalCreditModel creditModel;
            using (var context = creditContextFactory.CreateContext())
            {
                creditModel = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, isMortgageLoansEnabled: true, Clock.Now.DateTime);
            }

            if (creditModel.Status != CreditStatus.Normal.ToString())
            {
                failedMessage = $"Invalid status: {creditModel.Status}";
                termsChangeData = null;
                return false;
            }

            var currentNotNotifiedCapitalBalance = creditModel
                .Transactions
                .Where(x => x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                .Aggregate(0m, (a, b) => a + b.Amount);

            var interestBoundTo = newChangeTerms.NewInterestBoundFrom.AddMonths(newChangeTerms.NewFixedMonthsCount);
            var customersNewTotalInterest = newChangeTerms.NewReferenceInterestRatePercent + newChangeTerms.NewMarginInterestRatePercent.Value;

            termsChangeData = new T
            {
                AmortizationAmount = creditModel.AmortizationModel.GetActualFixedMonthlyPaymentOrException(),
                NewInterestRebindMonthCount = newChangeTerms.NewFixedMonthsCount,
                CustomersNewTotalInterest = customersNewTotalInterest,
                ReferenceInterest = newChangeTerms.NewReferenceInterestRatePercent,
                MarginInterest = newChangeTerms.NewMarginInterestRatePercent.Value,
                InterestBoundFrom = newChangeTerms.NewInterestBoundFrom,
                InterestBoundTo = interestBoundTo,
                CurrentCapitalBalance = creditModel.CurrentCapitalBalance
            };

            failedMessage = null;
            return true;
        }

        public (bool IsSuccess, string WarningMessage, CreditTermsChangeHeader TermChange) MlStartCreditTermsChange(string creditNr, MlNewChangeTerms newTerms, Func<IDocumentRenderer> createDocumentRenderer, ICustomerClient customerClient)
        {
            if (IsRejectedMarginInterestRate(newTerms.NewMarginInterestRatePercent))
                return (IsSuccess: false, WarningMessage: "Margin interest rate outside the allowed range.", TermChange: null);

            if (!TryComputeMlTermsChangeData(creditNr, newTerms, out MlTermsChangeData tc, out var failedMessage))
                return (IsSuccess: false, WarningMessage: failedMessage, TermChange: null);

            using (var context = creditContextFactory.CreateContext())
            {
                if (context
                    .CreditTermsChangeHeadersQueryable
                    .Any(x => x.CreditNr == creditNr && !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue))
                {
                    return (IsSuccess: false, WarningMessage: "There are pending term changes that need to be cancelled", TermChange: null);
                }

                var evt = this.AddBusinessEvent(BusinessEventType.StartedMlCreditTermsChange, context);

                var localH = new CreditTermsChangeHeader
                {
                    CreatedByEvent = evt,
                    CreditNr = creditNr,
                    AutoExpireDate = null,
                    ChangedById = this.UserId,
                    ChangedDate = this.Clock.Now,
                    InformationMetaData = this.InformationMetadata
                };

                void AddItem(int? applicantNr, CreditTermsChangeItem.CreditTermsChangeItemCode name, string value)
                {
                    context.AddCreditTermsChangeItems(new CreditTermsChangeItem
                    {
                        ApplicantNr = applicantNr,
                        CreatedByEvent = evt,
                        CreditTermsChange = localH,
                        ChangedById = this.UserId,
                        ChangedDate = this.Clock.Now,
                        InformationMetaData = this.InformationMetadata,
                        Name = name.ToString(),
                        Value = value
                    });
                };

                AddItem(
                 null,
                 CreditTermsChangeItem.CreditTermsChangeItemCode.NewInterestRebindMonthCount,
                 newTerms.NewFixedMonthsCount.ToString());

                AddItem(
                    null,
                    CreditTermsChangeItem.CreditTermsChangeItemCode.NewMarginInterestRatePercent,
                    newTerms.NewMarginInterestRatePercent.Value.ToString(CultureInfo.InvariantCulture));

                AddItem(
                    null, CreditTermsChangeItem.CreditTermsChangeItemCode.NewInterestBoundFromDate,
                    newTerms.NewInterestBoundFrom.ToString("yyyy-MM-dd"));

                AddItem(
                    null,
                    CreditTermsChangeItem.CreditTermsChangeItemCode.NewReferenceInterestRatePercent,
                    newTerms.NewReferenceInterestRatePercent.ToString(CultureInfo.InvariantCulture));

                var credit = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        x.CreditCustomers,
                        x.ProviderName
                    })
                    .Single();

                var customerIdByApplicantNr = credit.CreditCustomers.ToDictionary(x => x.ApplicantNr, x => x.CustomerId);
                var customerInfos = LoadAgreementApplicantInfo(customerIdByApplicantNr, customerClient);

                var attachmentArchiveKeys = new List<string>();

                string agreementArchiveKey;
                using (var documentRenderer = createDocumentRenderer())
                {
                    var printContext = GetCreditChangeTermsAgreementContext(creditNr, credit.CreditCustomers.Count, customerInfos, tc, customerClient);
                    agreementArchiveKey = documentRenderer.RenderDocumentToArchive("mortgageloan-change-terms", printContext, $"changeterms-{creditNr}-{Clock.Today.ToString("yyyyMMdd")}.pdf");
                    attachmentArchiveKeys.Add(agreementArchiveKey);
                    AddItem(null, CreditTermsChangeItem.CreditTermsChangeItemCode.UnsignedAgreementDocumentArchiveKey, agreementArchiveKey);
                }

                AddElectronicIdSignatureSession(agreementArchiveKey, creditNr, evt.EventType, AddItem,
                    customerInfos.Keys.Select(applicantNr => (CivicRegNr: customerInfos[applicantNr].civicRegNr, Email: customerInfos[applicantNr].email, 
                        SignerNr: applicantNr, CustomerId: customerIdByApplicantNr[applicantNr])).ToList());

                AddComment($"Change terms initiated.", BusinessEventType.StartedMlCreditTermsChange, context, creditNr: creditNr, attachment: new CreditCommentAttachmentModel { archiveKeys = attachmentArchiveKeys }, evt: evt);

                context.AddCreditTermsChangeHeaders(localH);

                context.SaveChanges();

                return (IsSuccess: true, WarningMessage: null, TermChange: localH);
            }
        }

        private CommonElectronicIdSignatureSession AddElectronicIdSignatureSession(string agreementArchiveKey, string creditNr, string eventType, 
            Action<int?, CreditTermsChangeItem.CreditTermsChangeItemCode, string> addItem,
            List<(string CivicRegNr, string Email, int SignerNr, int CustomerId)> signers)
        {
            var callbackToken = Guid.NewGuid().ToString();
            var serverToServerCallbackUrl =
                serviceRegistry.InternalServiceUrl("nCredit", $"Api/MortgageLoans/ChangeTerms/SignaturePostback/{callbackToken}");

            var session = WithCreditNrOnExceptionR(() =>
            {
                return customerClient.CreateElectronicIdSignatureSession(new NTech.ElectronicSignatures.SingleDocumentSignatureRequestUnvalidated
                {
                    DocumentToSignArchiveKey = agreementArchiveKey,
                    DocumentToSignFileName = $"change-terms-{creditNr}.pdf",
                    RedirectAfterSuccessUrl = serviceRegistry.ExternalServiceUrl("nCustomerPages", "signature-result/success").ToString(),
                    RedirectAfterFailedUrl = serviceRegistry.ExternalServiceUrl("nCustomerPages", "signature-result/failure").ToString(),
                    ServerToServerCallbackUrl = serverToServerCallbackUrl.ToString(),
                    SigningCustomers = signers.Select(x => new NTech.ElectronicSignatures.SingleDocumentSignatureRequestUnvalidated.SigningCustomer
                    {
                        SignerNr = x.SignerNr,
                        CivicRegNr = x.CivicRegNr
                    }).ToList(),
                    CustomData = new Dictionary<string, string> 
                    {
                        ["customerIdBySignerNr"] = JsonConvert.SerializeObject(signers.ToDictionary(x => x.SignerNr, x => x.CustomerId))
                    }
                });
            }, creditNr);
            var sessionKey = session.Id;
            var providerName = session.SignatureProviderName;

            addItem(null, CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureCallbackToken, callbackToken);
            addItem(null, CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureSessionKey, sessionKey);
            addItem(null, CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureProviderName, session.SignatureProviderName);

            if (emailServiceFactory.HasEmailProvider)
            {
                var emailSettings = cachedSettingsService.LoadSettings("mlStandardChangeTermsEmailTemplates");
                if(emailSettings.Opt("isEnabled") == "true")
                {
                    var emailService = emailServiceFactory.CreateEmailService();
                    var templateSubjectText = emailSettings.Opt("templateSubjectText");
                    var templateBodyText = emailSettings.Opt("templateBodyText");
                    foreach (var signer in signers.Where(x => x.Email != null))
                    {
                        try
                        {
                            var signatureUrl = session.GetActiveSignatureUrlBySignerNr().Opt(signer.SignerNr);
                            emailService.SendRawEmail(new List<string>() { signer.Email }, templateSubjectText, templateBodyText, new Dictionary<string, object> { { "link", signatureUrl } }, eventType);
                        }
                        catch (Exception ex)
                        {
                            loggingService.Error(ex, $"Change terms email failed for {creditNr}");
                        }
                    }
                }
            }

            return session;
        }

        private Dictionary<int, AgreementApplicantInfo> LoadAgreementApplicantInfo(Dictionary<int, int> customerIdByApplicantId, ICustomerClient customerClient)
        {
            var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(
                new HashSet<int>(customerIdByApplicantId.Values),
                "civicRegNr", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry", "email");

            var result = new Dictionary<int, AgreementApplicantInfo>();
            foreach (var c in customerIdByApplicantId)
            {
                var applicantNr = c.Key;
                var customerId = c.Value;

                var items = customerData[customerId];

                result[applicantNr] = new AgreementApplicantInfo
                {
                    civicRegNr = items.Opt("civicRegNr"),
                    fullName = ((items.Opt("firstName") ?? "") + " " + (items.Opt("lastName") ?? "")).Trim(),
                    streetAddress = items.Opt("addressStreet"),
                    areaAndZipcode = ((items.Opt("addressZipcode") ?? "") + " " + (items.Opt("addressCity") ?? "")).Trim(),
                    email = items.Opt("email")
                };
            }

            return result;
        }

        private class AgreementApplicantInfo
        {
            public string civicRegNr { get; set; }
            public string fullName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
        }

        private Dictionary<string, object> GetCreditChangeTermsAgreementContext(
        string creditNr, int nrOfApplicants, IDictionary<int, AgreementApplicantInfo> applicantInfoByApplicantNr, MlTermsChangeData tc, ICustomerClient customerClient)
        {
            var templateService = new StandardHtmlTemplateService(customerClient);

            return new Dictionary<string, object>
            {
                ["agreementDate"] = this.Clock.Today.ToString("d", this.PrintFormattingCulture),
                ["loanNumber"] = creditNr,
                ["loanAmount"] = tc.CurrentCapitalBalance?.ToString("C", this.PrintFormattingCulture),
                ["interestRateFixedUntil"] = tc.InterestBoundTo?.ToString("d", this.PrintFormattingCulture) ?? "-",
                ["interestRate"] = tc.CustomersNewTotalInterest.ToString(this.PrintFormattingCulture),
                ["amortizationAmount"] = tc.AmortizationAmount.ToString("C", this.PrintFormattingCulture),
                ["customers"] = Enumerable.Range(1, nrOfApplicants)
                    .Select(x => applicantInfoByApplicantNr[x])
                    .ToList(),
                ["interestRebindMonthCount"] = tc.NewInterestRebindMonthCount < 12 ?
                    tc.NewInterestRebindMonthCount.ToString(this.PrintFormattingCulture) + " mån"
                    : Math.Round(tc.NewInterestRebindMonthCount / 12m, 0).ToString(this.PrintFormattingCulture) + " år",

                ["GeneralTermsRawHtml"] = templateService.BuildWeasyPrintHtmlFromSettingsTemplate("generalTermsHtmlTemplate")
            };
        }

        public bool TryScheduleCreditTermsChange(int id, out string failedMessage)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                if (!TryFetchPendingTermsChangeData(id, out var pendingData, out failedMessage))
                    return false;

                if (pendingData.ScheduledDate.HasValue)
                {
                    failedMessage = "Already scheduled";
                    return false;
                }

                var termChange = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new
                    {
                        x.Id,
                        x.CreditNr,
                        CreditStatus = x.Credit.Status,
                    })
                    .Single(x => x.Id == id);

                if (termChange.CreditStatus != CreditStatus.Normal.ToString())
                {
                    failedMessage = $"Invalid credit status: {termChange.CreditStatus}";
                    return false;
                }

                context.AddCreditTermsChangeItems(context.FillInfrastructureFields(new CreditTermsChangeItem
                {
                    CreatedByEvent = AddBusinessEvent(BusinessEventType.ScheduledMlCreditTermsChange, context),
                    ApplicantNr = null,
                    CreditTermsChangeHeaderId = termChange.Id,
                    Name = CreditTermsChangeItem.CreditTermsChangeItemCode.MlScheduledDate.ToString(),
                    Value = context.CoreClock.Now.ToString("o")
                }));


                AddComment($"Change terms scheduled.", BusinessEventType.ScheduledMlCreditTermsChange, context, creditNr: termChange.CreditNr);

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        public bool AttachSignedAgreement(int id, string archiveKey)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var header = context
                    .CreditTermsChangeHeadersQueryable
                    .SingleOrDefault(x => x.Id == id);

                if (header == null || header.CancelledByEventId.HasValue || header.CommitedByEventId.HasValue)
                    throw new NTechCoreWebserviceException("Term change missing, commited or cancelled") { ErrorCode = "missingOrClosed", ErrorHttpStatusCode = 400, IsUserFacing = true };

                var evt = this.AddBusinessEvent(BusinessEventType.AddedSignedAgreementToCreditTermsChange, context);

                AddComment($"Signed agreement - document added.", BusinessEventType.AddedSignedAgreementToCreditTermsChange, context, creditNr: header.CreditNr);

                context.AddCreditTermsChangeItems(context.FillInfrastructureFields(new CreditTermsChangeItem
                {
                    CreatedByEvent = evt,
                    CreditTermsChange = header,
                    Name = CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString(),
                    Value = archiveKey
                }));

                context.SaveChanges();
            }

            return true;
        }

        public bool RemoveSignedAgreeement(int id, string archiveKey)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var result = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new { Header = x, Items = x.Items })
                    .SingleOrDefault(x => x.Header.Id == id);
                var header = result?.Header;

                if (header == null || header.CancelledByEventId.HasValue || header.CommitedByEventId.HasValue)
                    throw new NTechCoreWebserviceException("Term change missing, commited or cancelled") { ErrorCode = "missingOrClosed", ErrorHttpStatusCode = 400, IsUserFacing = true };

                var signedAgreementItem = result.Items
                    .Where(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString() && x.Value == archiveKey)
                    .FirstOrDefault();

                context.RemoveCreditTermsChangeItems(signedAgreementItem);

                AddComment($"Signed agreement - document removed.", BusinessEventType.RemovedSignedAgreementToCreditTermsChange, context, creditNr: header.CreditNr);

                context.SaveChanges();
            }

            return true;
        }

        public bool TryAcceptCreditTermsChange(int id, out string failedMessage)
        {
            if (!TryFetchPendingTermsChangeData(id, out PendingChangeData pendingChangeData, out failedMessage))
                return false;

            using (var context = creditContextFactory.CreateContext())
            {
                var creditHeader = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new
                    {
                        Header = x,
                        x.Credit,
                        Items = x.Items.Select(y => new
                        {
                            y.Name,
                            y.Value
                        })
                    })
                    .Single(x => x.Header.Id == id);

                var evt = AddBusinessEvent(BusinessEventType.AcceptedCreditTermsChange, context);
                creditHeader.Header.CommitedByEvent = evt;

                if (!AcceptCreditTermChange(context, creditHeader.Credit, pendingChangeData.NewInterestRebindMonthCount, pendingChangeData.MarginInterest, evt, out string failedMessageStr))
                {
                    failedMessage = failedMessageStr;
                    return false;
                }

                var signedAgreementDocumentArchiveKey = creditHeader
                    .Items
                    .Where(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString())
                    .Select(x => x.Value)
                    .First();

                AddCreditDocument("MortgageLoanChangeTermsAgreement", applicantNr: null, signedAgreementDocumentArchiveKey, context, creditHeader.Credit.CreditNr);

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        public bool TryAcceptDefaultCreditTermsChange(string creditNr, out string failedMessage)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var creditHeader = context
                    .CreditHeadersQueryable
                    .Where(c => c.Status == CreditStatus.Normal.ToString())
                    .Select(x => new
                    {
                        Credit = x,
                        x.CreditNr
                    })
                    .Single(y => y.CreditNr == creditNr);

                var evt = AddBusinessEvent(BusinessEventType.MlDefaultCreditTermsChange, context);

                var creditData = new PartialCreditModelRepository().NewQuery(Clock.Today)
                    .WithValues(DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate, DatedCreditValueCode.RequestedMarginInterestRate)
                    .Execute(context, x => x.Where(y => y.Credit.CreditNr == creditHeader.Credit.CreditNr))
                    .Single();

                var currentMarginInterestRate = creditData.GetValue(DatedCreditValueCode.MarginInterestRate).Value;

                if (!AcceptCreditTermChange(context, creditHeader.Credit, DefaultInterestBindMonthCount, currentMarginInterestRate, evt, out string failedMessageStr, true))
                {
                    failedMessage = failedMessageStr;
                    return false;
                }

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        private bool AcceptCreditTermChange(ICreditContextExtended context, CreditHeader credit, int newInterestRebindMonthCount, decimal marginInterest, BusinessEvent evt, out string failedMessage, bool isDefaultTerms = false)
        {
            if (credit.Status != CreditStatus.Normal.ToString())
            {
                failedMessage = $"Invalid credit status: {credit.Status}";
                return false;
            }

            DateTime interestBoundTo;
            var additionalCommentText = "";
            if (!IsCurrentReferenceRateExists(newInterestRebindMonthCount))
            {
                newInterestRebindMonthCount = DefaultInterestBindMonthCount;
                interestBoundTo = Clock.Today.AddMonths(DefaultInterestBindMonthCount);
                additionalCommentText += $" Scheduled reference interest month count does not exist, defaulted to {DefaultInterestBindMonthCount} months.";
            }
            else
            {
                interestBoundTo = Clock.Today.AddMonths(newInterestRebindMonthCount);
            }

            var creditData = new PartialCreditModelRepository().NewQuery(Clock.Today)
                               .WithValues(DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate, DatedCreditValueCode.RequestedMarginInterestRate)
                               .Execute(context, x => x.Where(y => y.Credit.CreditNr == credit.CreditNr))
                               .Single();

            var currentMarginInterestRate = creditData.GetValue(DatedCreditValueCode.MarginInterestRate).Value;
            var currentRequestedMarginInterestRate = creditData.GetValue(DatedCreditValueCode.RequestedMarginInterestRate);
            var newReferenceInterestRate = GetCurrentReferenceInterestRateForMonthCount(newInterestRebindMonthCount);
            var interestChange = this.legalInterestCeilingService.HandleMarginInterestRateChange(
                                newReferenceInterestRate,
                                currentRequestedMarginInterestRate,
                                currentMarginInterestRate,
                                isDefaultTerms
                                    ? (currentRequestedMarginInterestRate ?? currentMarginInterestRate)
                                    : marginInterest);

            BusinessEventType eventType;
            if (!isDefaultTerms)
            {
                eventType = BusinessEventType.AcceptedCreditTermsChange;
            }
            else
            {
                eventType = BusinessEventType.MlDefaultCreditTermsChange;
                additionalCommentText += " Default terms used.";
            }

            if (interestChange.NewMarginInterestRate.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.MarginInterestRate.ToString(), interestChange.NewMarginInterestRate.Value, credit, evt, context);
                additionalCommentText += $" New margin interest is {interestChange.NewMarginInterestRate.Value / 100m:P}.";
            }

            if (interestChange.NewRequestedMarginInterestRate.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.RequestedMarginInterestRate.ToString(), interestChange.NewRequestedMarginInterestRate.Value, credit, evt, context);
                additionalCommentText += $" New requested margin interest is {interestChange.NewRequestedMarginInterestRate.Value / 100m:P}.";
            }

            AddComment($"Change terms accepted." + additionalCommentText, eventType, context, creditNr: credit.CreditNr);
            AddDatedCreditValue(DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString(), newInterestRebindMonthCount, credit, evt, context);
            AddDatedCreditValue(DatedCreditValueCode.ReferenceInterestRate.ToString(), newReferenceInterestRate, credit, evt, context);
            AddDatedCreditDate(DatedCreditDateCode.MortgageLoanNextInterestRebindDate, interestBoundTo, evt, context, credit.CreditNr, credit);

            failedMessage = "";

            return true;
        }

        private bool IsRejectedMarginInterestRate(decimal? marginInterestRatePercent)
        {
            if (!marginInterestRatePercent.HasValue)
                return false; //Missing data is handled elsewhere

            var m = envSettings.MinAndMaxAllowedMarginInterestRate;
            if (m == null || (!m.Item1.HasValue && !m.Item2.HasValue))
                return false;

            if (m.Item1.HasValue && marginInterestRatePercent.Value < m.Item1.Value)
                return true;

            if (m.Item2.HasValue && marginInterestRatePercent.Value > m.Item2.Value)
                return true;

            return false;
        }

        private bool IsCurrentReferenceRateExists(int forMonthCount)
        {
            return GetCurrentRates()?
                .Where(x => x.MonthCount == forMonthCount)
                .Select(y => y.RatePercent)
                .Count() > 0;
        }

        private decimal GetCurrentReferenceInterestRateForMonthCount(int forMonthCount)
        {
            return GetCurrentRates()?
                .Where(x => x.MonthCount == forMonthCount)
                .Select(y => y.RatePercent)
                .FirstOrDefault() ?? 0m;
        }

        private List<FixedMortgageLoanInterestRateBase> GetCurrentRates()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return context.FixedMortgageLoanInterestRatesQueryable
                    .ToList()
                    .Cast<FixedMortgageLoanInterestRateBase>()
                    .ToList();
            }
        }

        public (List<string> UpdatedPendingChange, List<string> UpdatedDefault) UpdateChangeTerms()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var today = context.CoreClock.Today;
                var credits = context
                    .CreditHeadersQueryable
                    .Select(x => new
                    {
                        x.CreditNr,
                        ActiveScheduledTermChange = x
                            .TermsChanges
                            .Where(y => y.CommitedByEvent == null && y.CancelledByEvent == null
                                && y.Items.Any(z => z.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.MlScheduledDate.ToString()))
                            .OrderByDescending(y => y.CreatedByEventId)
                            .Select(y => new
                            {
                                y.Id
                            })
                            .FirstOrDefault(),
                        InterestRebindMonthCount = x
                                    .DatedCreditValues
                                    .Where(z => z.Name == DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString())
                                    .OrderByDescending(z => z.BusinessEventId)
                                    .Select(z => z.Value)
                                    .FirstOrDefault(),
                        NextInterestRebindDate = x
                                    .DatedCreditDates
                                    .Where(z => z.Name == DatedCreditDateCode.MortgageLoanNextInterestRebindDate.ToString())
                                    .OrderByDescending(z => z.BusinessEventId)
                                    .Select(z => z.RemovedByBusinessEventId == null ? (DateTime?)z.Value : null)
                                    .FirstOrDefault(), 
                        x.Status
                    })
                    .Where(x => x.Status == CreditStatus.Normal.ToString() && (x.ActiveScheduledTermChange != null || x.NextInterestRebindDate <= today))
                    .ToList();

                var updatedPendingChange = new List<string>();
                var updatedDefault = new List<string>();

                foreach (var credit in credits)
                {
                    if (credit.ActiveScheduledTermChange != null)
                    {
                        if (!TryFetchPendingTermsChangeData(credit.ActiveScheduledTermChange.Id, out var pendingChange, out var failedMessage))
                        {
                            loggingService.Error($"TryFetchPendingTermsChangeData in UpdateTerms failed on {credit.CreditNr}: {failedMessage}");
                            continue;
                        }

                        if (pendingChange.InterestBoundFrom == null || pendingChange.InterestBoundFrom <= Clock.Today)
                        {
                            if (!TryAcceptCreditTermsChange(credit.ActiveScheduledTermChange.Id, out var failedMessage2))
                            {
                                loggingService.Error($"TryAcceptCreditTermsChange in UpdateTerms failed on {credit.CreditNr}: {failedMessage2}");
                                continue;
                            }
                            updatedPendingChange.Add(credit.CreditNr);
                        }
                    }
                    else
                    {
                        //Passed date with no term change
                        if (!TryAcceptDefaultCreditTermsChange(credit.CreditNr, out string failedMessage))
                        {
                            loggingService.Error($"TryAcceptDefaultCreditTermsChange in UpdateTerms failed on {credit.CreditNr}: {failedMessage}");
                        }
                        else
                        {
                            updatedDefault.Add(credit.CreditNr);
                        }
                    }
                }

                return (UpdatedPendingChange: updatedPendingChange, UpdatedDefault: updatedDefault);
            }
        }

        public bool TryUpdateTermsChangeOnSignatureEvent(string signatureCallbackToken, string signatureEventName, string signatureErrorMessage, out string failedMessage)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var result = TryUpdateTermsChangeOnSignatureEventI(context, signatureCallbackToken, signatureEventName, signatureErrorMessage, out failedMessage);
                context.SaveChanges();
                return result;
            }
        }

        private bool TryUpdateTermsChangeOnSignatureEventI(ICreditContextExtended context, string signatureCallbackToken, string signatureEventName, string signatureErrorMessage, out string failedMessage)
        {
            var h = context
                .CreditTermsChangeHeadersQueryable
                .Select(x => new
                {
                    Header = x,
                    Items = x.Items,
                    Credit = x.Credit,
                    HitTokenItem = x
                        .Items
                        .Where(y => y.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureCallbackToken.ToString() && y.Value == signatureCallbackToken)
                        .FirstOrDefault()
                })
                .Where(x => x.HitTokenItem != null)
                .SingleOrDefault();

            if (h == null)
            {
                failedMessage = "No terms change exists with that token";
                return false;
            }

            if (signatureEventName == "Success" || signatureEventName == "Failed")
            {
                var failedEventType = BusinessEventType.AddedSignedAgreementToCreditTermsChange + "_Failed";
                if (h.Header.CancelledByEventId.HasValue)
                {
                    failedMessage = "This signing session has been cancelled";
                    AddComment($"Change terms - Signature callback failed since the term change is cancelled.", failedEventType, h.Credit, context);
                    return false;
                }
                if (h.Header.CommitedByEventId.HasValue)
                {
                    failedMessage = "This signing session has been commited";
                    AddComment($"Change terms - Signature callback for failed since the term change is commited.", failedEventType, h.Credit, context);
                    return false;
                }
                var signatureSessionKeyItem = h.Items.Single(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureSessionKey.ToString());


                var signatureSession = WithCreditNrOnExceptionR(() => customerClient.GetElectronicIdSignatureSession(signatureSessionKeyItem.Value, false), h.Header.CreditNr)?.Session;

                if (signatureSession == null)
                {
                    failedMessage = "Session does not exist";
                    AddComment($"Change terms - Sesion does not exist", failedEventType, h.Credit, context);
                    return false;
                }
                if (!signatureSession.HaveAllSigned())
                {
                    if (signatureEventName == "Failed")
                    {
                        return TryCancelCreditTermsChange(
                            h.Header.Id,
                            false,
                            out failedMessage,
                            additionalReasonMessage: $" Signature failed: {signatureErrorMessage}");
                    }
                    else
                    {
                        //Waiting for others to sign.
                        failedMessage = null;
                        return true;
                    }
                }
                if (signatureSession.SignedPdf == null)
                {
                    failedMessage = "Missing signed document";
                    AddComment($"Change terms - Signature callback failed. Signed document is missing.", failedEventType, h.Credit, context);
                    return false;
                }
                if (h.Items.Any(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString()))
                {
                    failedMessage = "Already signed";
                    AddComment($"Change terms - Signature callback failed. Already signed.", failedEventType, h.Credit, context);
                    return false;
                }

                var documentFileName = $"changeterms-{h.Header.CreditNr}-{Clock.Today.ToString("yyyy-MM-dd")}-signed.pdf";

                var evt = AddBusinessEvent(BusinessEventType.AddedSignedAgreementToCreditTermsChange, context);

                context.AddCreditTermsChangeItems(new CreditTermsChangeItem
                {
                    CreatedByEvent = evt,
                    CreditTermsChange = h.Header,
                    Name = CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString(),
                    Value = signatureSession.SignedPdf.ArchiveKey,
                    InformationMetaData = this.InformationMetadata,
                    ChangedById = this.UserId,
                    ChangedDate = this.Clock.Now,
                });

                AddComment(
                    $"Change terms - agreement signed",
                    BusinessEventType.AddedSignedAgreementToCreditTermsChange, context,
                    creditNr: h.Credit.CreditNr,
                    attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(new List<string> { signatureSession.SignedPdf.ArchiveKey }));

                //Cancel the session
                WithCreditNrOnExceptionR(() => customerClient.GetElectronicIdSignatureSession(signatureSessionKeyItem.Value, true), h.Header.CreditNr);
                var itemsToRemove = new[] { CreditTermsChangeItemCode.SignatureCallbackToken, CreditTermsChangeItemCode.SignatureSessionKey, CreditTermsChangeItemCode.SignatureProviderName }
                    .Select(x => x.ToString()).ToList();

                //Remove items tracking the session since the user can backtrack by removing the signed document
                h.Items.Where(x => itemsToRemove.Contains(x.Name)).ToList()
                    .ForEach(context.RemoveCreditTermsChangeItems);

                failedMessage = null;
                return true;
            }
            else
            {
                loggingService.Information($"Credit change terms event ignored '{signatureEventName}' '{signatureCallbackToken}'");
                failedMessage = null;
                return true;
            }
        }
    }

    public class MlNewChangeTerms
    {
        public int NewFixedMonthsCount { get; set; }
        public decimal? NewMarginInterestRatePercent { get; set; }
        public DateTime NewInterestBoundFrom { get; set; }
        public decimal NewReferenceInterestRatePercent { get; set; }
    }

    public class MlTermsChangeData
    {
        public decimal AmortizationAmount { get; set; }
        public int NewInterestRebindMonthCount { get; set; }
        public decimal CustomersNewTotalInterest { get; set; }
        public decimal ReferenceInterest { get; set; }
        public decimal MarginInterest { get; set; }
        public DateTime? InterestBoundFrom { get; set; }
        public DateTime? InterestBoundTo { get; set; }
        public decimal? CurrentCapitalBalance { get; set; }
    }

    public class PendingChangeData : MlTermsChangeData
    {
        public int Id { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public SignatureItem Signature { get; set; }
        public string ActiveSignatureSessionKey { get; set; }

        public class SignatureItem
        {
            public string UnsignedDocumentKey { get; set; }
            public string SignedDocumentKey { get; set; }
            public DateTime? SignatureDate { get; set; }
            public string SignatureUrl { get; set; }
        }
    }
}