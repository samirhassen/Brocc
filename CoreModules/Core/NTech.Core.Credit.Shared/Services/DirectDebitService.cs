using nCredit.DbModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.Autogiro;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static nCredit.Code.Services.DirectDebitCreditDetails;
using static nCredit.DbModel.DirectDebitBusinessEventManager;

namespace nCredit.Code.Services
{
    public class DirectDebitService : IDirectDebitService
    {
        private readonly ICoreClock clock;
        private readonly IBankAccountValidationService bankAccountValidation;
        private readonly DirectDebitBusinessEventManager directDebitBusinessEventManager;
        private readonly IUserDisplayNameService userDisplayNameService;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CreditContextFactory contextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICustomerClient customerClient;
        private readonly IDocumentClient documentClient;

        public DirectDebitService(ICoreClock clock, IBankAccountValidationService bankAccountValidation, DirectDebitBusinessEventManager directDebitBusinessEventManager, 
            IUserDisplayNameService userDisplayNameService, PaymentAccountService paymentAccountService, CreditContextFactory contextFactory, ICreditEnvSettings envSettings, 
            IClientConfigurationCore clientConfiguration, ICustomerClient customerClient, IDocumentClient documentClient)
        {
            this.clock = clock;
            this.bankAccountValidation = bankAccountValidation;
            this.directDebitBusinessEventManager = directDebitBusinessEventManager;
            this.userDisplayNameService = userDisplayNameService;
            this.paymentAccountService = paymentAccountService;
            this.contextFactory = contextFactory;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
            this.customerClient = customerClient;
            this.documentClient = documentClient;
        }

        private Tuple<bool, DateTime> GetActiveStateAndDate(CreditDomainModel model)
        {
            DateTime? activeSinceDate = null;
            var isActive = model.GetIsDirectDebitActive(clock.Today, observeTransactionDate: x => activeSinceDate = x);

            return Tuple.Create(isActive, activeSinceDate ?? model.GetStartDate().DateTime.Date);
        }

        public Dictionary<int, int> FetchCustomerIdsByCreditNr(string creditNr)
        {
            using (var context = contextFactory.CreateContext())
            {
                return context
                    .CreditCustomersQueryable
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        x.ApplicantNr,
                        x.CustomerId
                    })
                    .ToList()
                    .ToDictionary(x => x.ApplicantNr, x => x.CustomerId);
            }
        }

        private CreditDomainModel GetCreditDomainModelByCreditNr(string creditNr)
        {
            using(var context = contextFactory.CreateContext())
            {
                return CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);
            }
        }

        public DirectDebitCreditDetails FetchCreditDetails(string creditNr, INTechCurrentUserMetadata currentUser, string backTarget = null)
        {
            var model = GetCreditDomainModelByCreditNr(creditNr);

            var customerIds = FetchCustomerIdsByCreditNr(creditNr);

            var activeStateAndDate = GetActiveStateAndDate(model);
            var isActive = activeStateAndDate.Item1;
            var isActiveStateDate = activeStateAndDate.Item2;

            var accountOwnerApplicantNr = model.GetDirectDebitAccountOwnerApplicantNr(clock.Today);
            var bankAccountNr = model.GetDirectDebitBankAccountNr(clock.Today);

            var nrOfApplicants = model.GetNrOfApplicants();

            var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(
                new HashSet<int>(customerIds.Values),
                "firstName", "civicRegNr");

            var g = new AutogiroPaymentNumberGenerator();

            var civicRegNrParser = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry);


            var applicants = new List<ApplicantModel>();
            foreach (var applicantNr in Enumerable.Range(1, nrOfApplicants))
            {
                var customerId = customerIds[applicantNr];
                applicants.Add(new ApplicantModel
                {
                    ApplicantNr = applicantNr,
                    CustomerId = customerId,
                    FirstName = customerData?.Opt(customerId)?.Opt("firstName"),
                    BirthDate = civicRegNrParser.Parse(customerData[customerId]["civicRegNr"]).BirthDate,
                    StandardPaymentNr = g.GenerateNr(model.CreditNr, applicantNr)
                });
            }

            return new DirectDebitCreditDetails
            {
                CreditNr = creditNr,
                IsActive = isActive,
                SchedulationChangesModel = GetSchedulationChangesModel(creditNr, applicants),
                CurrentIsActiveStateDate = isActiveStateDate,
                BankAccount = bankAccountNr == null ? null : bankAccountValidation.ValidateBankAccountNr(bankAccountNr.PaymentFileFormattedNr),
                AccountOwnerApplicantNr = accountOwnerApplicantNr == null ? null : accountOwnerApplicantNr,
                Applicants = applicants
            };
        }

        private SchedulationModel GetSchedulationChangesModel(string creditNr, List<ApplicantModel> applicants)
        {
            using (var context = contextFactory.CreateContext())
            {
                var schedulationModel = context.CreditOutgoingDirectDebitItemsQueryable.Where(x => x.CreditNr == creditNr).OrderByDescending(y => y.ChangedDate).FirstOrDefault();
                var schedulationAccountOwner = applicants?.Where(x => x.CustomerId == schedulationModel?.BankAccountOwnerCustomerId).FirstOrDefault();
                var pendingSchedulationModel = context.KeyValueItemsQueryable.Where(x => x.Key == creditNr && x.KeySpace == "InternalDirectDebitFileOperation").FirstOrDefault();

                return new SchedulationModel
                {
                    SchedulationDetails = new SchedulationDetailsModel
                    {
                        SchedulationOperation = schedulationModel?.Operation,
                        AccountOwnerApplicantNr = schedulationAccountOwner == null ? null : (int?)schedulationAccountOwner.ApplicantNr,
                        BankAccount = schedulationModel?.BankAccountNr == null ? null : bankAccountValidation.ValidateBankAccountNr(schedulationModel?.BankAccountNr),
                        PaymentNr = schedulationModel?.PaymentNr
                    },
                    PendingSchedulationDetails = pendingSchedulationModel == null ? new SchedulationDetailsModel() : JsonConvert.DeserializeObject<SchedulationDetailsModel>(pendingSchedulationModel?.Value)
                };
            }
        }

        public bool TryUpdateDirectDebitCheckStatusState(string creditNr, string newStatus, string bankAccountNr, int? bankAccountOwnerApplicantNr, INTechCurrentUserMetadata currentUser, out string failedMessage)
        {
            failedMessage = null;

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "Missing creditNr";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                failedMessage = "Missing newStatus";
                return false;
            }

            if (!newStatus.IsOneOf("Active", "NotActive"))
            {
                failedMessage = "Invalid newStatus";
                return false;
            }

            if (newStatus == "Active")
            {
                BankAccountNumberSe b = null;
                if (!string.IsNullOrWhiteSpace(bankAccountNr))
                {
                    if (!bankAccountOwnerApplicantNr.HasValue || bankAccountOwnerApplicantNr.Value <= 0)
                    {
                        failedMessage = "Missing bankAccountOwnerApplicantNr";
                        return false;
                    }

                    string bankAccountFailedMessage;
                    if (!BankAccountNumberSe.TryParse(bankAccountNr, out b, out bankAccountFailedMessage))
                    {
                        failedMessage = $"Invalid bankAccountNr - {bankAccountFailedMessage}";
                        return false;
                    }
                }

                var mgr = directDebitBusinessEventManager;

                using (var context = contextFactory.CreateContext())
                {
                    if (!mgr.TryChangeDirectDebitStatus(context, creditNr, newStatus == "Active", b, bankAccountOwnerApplicantNr, out failedMessage))
                        return false;

                    context.SaveChanges();
                }

                return true;
            }

            else
            {
                var mgr = directDebitBusinessEventManager;

                using (var context = contextFactory.CreateContext())
                {
                    if (!mgr.TryChangeDirectDebitStatus(context, creditNr, newStatus == "Active", null, null, out failedMessage))
                        return false;

                    context.SaveChanges();
                }

                return true;
            }
        }


        public List<DirectDebitEventModel> FetchEvents(string creditNr)
        {
            using (var context = contextFactory.CreateContext())
            {
                var events = new List<DirectDebitEventModel>();

                Func<IEnumerable<DatedCreditString>, DatedCreditStringCode, string> getDatedStringValue = (strings, code) =>
                    strings.SingleOrDefault(x => x.Name == code.ToString())?.Value;

                Action<IEnumerable<DatedCreditString>, string> handleStandardEvent = (strings, eventDescription) =>
                {
                    foreach (var ns in strings.GroupBy(x => x.BusinessEventId))
                    {
                        var eventTemplate = ns.First();
                        var eventDate = eventTemplate.TransactionDate;

                        var isDirectDebitActive = getDatedStringValue(ns, DatedCreditStringCode.IsDirectDebitActive);
                        var accountOwnerApplicantNr = getDatedStringValue(ns, DatedCreditStringCode.DirectDebitAccountOwnerApplicantNr);
                        var bankAccountNr = getDatedStringValue(ns, DatedCreditStringCode.DirectDebitBankAccountNr);
                        var ts = new List<string>();
                        if (isDirectDebitActive != null)
                            ts.Add(isDirectDebitActive == "true" ? "Activated" : "Deactivated");
                        if (accountOwnerApplicantNr != null)
                            ts.Add($"Bank account owner set to applicant {accountOwnerApplicantNr}");
                        if (bankAccountNr != null)
                            ts.Add($"Bank account nr set to {bankAccountNr}");
                        events.Add(new DirectDebitEventModel
                        {
                            BusinessEventId = ns.Key,
                            Date = eventDate,
                            UserId = eventTemplate.ChangedById.ToString(),
                            LongText = $"{eventDescription}: {string.Join(", ", ts)}"
                        });
                    }
                };

                var c = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        Customers = x.CreditCustomers.Select(y => new
                        {
                            y.ApplicantNr,
                            y.CustomerId
                        }),
                        ChangeDirectDebitStatusStrings = x.DatedCreditStrings.Where(y => y.BusinessEvent.EventType == BusinessEventType.ChangeDirectDebitStatus.ToString()),
                        PlacedUnplacedIncomingPaymentDirectDebitStatusStrings = x.DatedCreditStrings.Where(y => y.BusinessEvent.EventType == BusinessEventType.PlacedUnplacedIncomingPayment.ToString()),
                        NewMortgageLoanStrings = x.DatedCreditStrings.Where(y => y.BusinessEvent.EventType == BusinessEventType.NewMortgageLoan.ToString()),
                        CreditOutgoingDirectDebitItems = x.CreditOutgoingDirectDebitItems.Select(y => new { Item = y, CreatedByEvent = y.CreatedByEvent, SentInFileEvent = y.OutgoingDirectDebitStatusChangeFile.CreatedByEvent }),
                        ImportedIncomingDirectDebitChangeFileStrings = x.DatedCreditStrings.Where(y => y.BusinessEvent.EventType == BusinessEventType.ImportedIncomingDirectDebitChangeFile.ToString()),
                        ImportedIncomingDirectDebitChangeFileComments = x.Comments.Where(y => y.CreatedByEvent.EventType == BusinessEventType.ImportedIncomingDirectDebitChangeFile.ToString()).Select(y => new { Comment = y, Event = y.CreatedByEvent })
                    })
                    .Single();

                var applicantNrByCustomerId = c.Customers.ToDictionary(x => x.CustomerId, x => x.ApplicantNr);

                handleStandardEvent(c.NewMortgageLoanStrings, "New Loan");
                handleStandardEvent(c.ChangeDirectDebitStatusStrings, "Change internal status");
                handleStandardEvent(c.ImportedIncomingDirectDebitChangeFileStrings, "Incoming status file");
                handleStandardEvent(c.PlacedUnplacedIncomingPaymentDirectDebitStatusStrings, "Settlement payment");

                //Scheduled direct debit events
                foreach (var s in c.CreditOutgoingDirectDebitItems)
                {
                    var e = s.CreatedByEvent;
                    var i = s.Item;
                    var ts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(i.PaymentNr))
                        ts.Add($"Paymentnr {i.PaymentNr}");
                    if (!string.IsNullOrWhiteSpace(i.ClientBankGiroNr))
                        ts.Add($"Bankgironr {i.ClientBankGiroNr}");
                    if (!string.IsNullOrWhiteSpace(i.BankAccountNr))
                        ts.Add($"Bank account {i.BankAccountNr}");
                    if (i.BankAccountOwnerCustomerId.HasValue)
                    {
                        if (applicantNrByCustomerId.ContainsKey(i.BankAccountOwnerCustomerId.Value))
                            ts.Add($"Account owner applicant nr {applicantNrByCustomerId[i.BankAccountOwnerCustomerId.Value]}");
                        else
                            ts.Add($"Account owner customer id {applicantNrByCustomerId.Values}"); //This should be impossible but if we forget to change this and allow externals this might happen. Change to allow clicking through to civicregnr if it happens
                    }

                    var automatedByEventText = e.EventType == BusinessEventType.ScheduledOutgoingDirectDebitChange.ToString() ? "" : $" (automated by {e.EventType})";

                    events.Add(new DirectDebitEventModel
                    {
                        BusinessEventId = e.Id,
                        Date = e.TransactionDate,
                        UserId = e.ChangedById.ToString(),
                        LongText = $"Scheduled {i.Operation}: {string.Join(", ", ts)}" + automatedByEventText
                    });

                    if (s.SentInFileEvent != null)
                    {
                        events.Add(new DirectDebitEventModel
                        {
                            BusinessEventId = s.SentInFileEvent.Id,
                            Date = s.SentInFileEvent.TransactionDate,
                            UserId = s.SentInFileEvent.ChangedById.ToString(),
                            LongText = $"Sent {i.Operation}: {string.Join(", ", ts)}"
                        });
                    }
                }

                //Incoming direct debit status change files skipped items
                foreach (var m in c.ImportedIncomingDirectDebitChangeFileComments)
                {
                    events.Add(new DirectDebitEventModel
                    {
                        BusinessEventId = m.Event.Id,
                        Date = m.Event.TransactionDate,
                        UserId = m.Event.ChangedById.ToString(),
                        LongText = $"Incoming status file: {m.Comment.CommentText}"
                    });
                }

                foreach (var e in events)
                {
                    e.UserDisplayName = userDisplayNameService?.GetUserDisplayNameByUserId(e.UserId);
                }

                return events.OrderByDescending(x => x.BusinessEventId).ToList();
            }
        }

        public bool TryScheduleDirectDebitActivation(string creditNr, string bankAccountNr, string paymentNr, int? customerId, INTechCurrentUserMetadata currentUser, bool? isChangeActivated, out string failedMessage)
        {
            if (!BankAccountNumberSe.TryParse(bankAccountNr, out BankAccountNumberSe b, out failedMessage))
                return false;

            var mgr = directDebitBusinessEventManager;

            using (var context = contextFactory.CreateContext())
            {
                if (isChangeActivated != true)
                {
                    if (!TrySetPendingSchedulationKeyValueItem(context, InternalDirectDebitFileOperation.PendingActivation, creditNr, customerId, bankAccountNr, paymentNr, currentUser, out failedMessage))
                        return false;

                    context.SaveChanges();
                }

                else
                {
                    if (!mgr.TryScheduleDirectDebitActivation(context, creditNr, b, paymentNr, customerId, paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro(), out failedMessage))
                        return false;

                    context.SaveChanges();
                }
            }

            failedMessage = null;
            return true;
        }

        private bool TrySetPendingSchedulationKeyValueItem(ICreditContextExtended context, InternalDirectDebitFileOperation operation, string creditNr, int? customerId, string bankAccountNr, string paymentNr, INTechCurrentUserMetadata currentUser, out string failedMessage)
        {
            var schedulation = new SchedulationDetailsModel
            {
                SchedulationOperation = operation.ToString(),
                AccountOwnerApplicantNr = context.CreditCustomersQueryable.Where(x => x.CreditNr == creditNr && x.CustomerId == customerId).Select(y => y.ApplicantNr).FirstOrDefault(),
                BankAccount = string.IsNullOrWhiteSpace(bankAccountNr) ? null : bankAccountValidation.ValidateBankAccountNr(bankAccountNr),
                PaymentNr = paymentNr
            };

            KeyValueStoreService.SetValueComposable(context, creditNr, "InternalDirectDebitFileOperation", JsonConvert.SerializeObject(schedulation));

            failedMessage = null;
            return true;
        }

        public bool TryScheduleDirectDebitChange(string creditNr, string bankAccountNr, string paymentNr, int? customerId, INTechCurrentUserMetadata currentUser, bool? isChangeActivated, string currentStatus, out string failedMessage)
        {
            if (!BankAccountNumberSe.TryParse(bankAccountNr, out BankAccountNumberSe b, out failedMessage))
                return false;

            var mgr = directDebitBusinessEventManager;

            using (var context = contextFactory.CreateContext())
            {
                if (isChangeActivated != true)
                {

                    if (!TrySetPendingSchedulationKeyValueItem(context, InternalDirectDebitFileOperation.PendingChange, creditNr, customerId, bankAccountNr, paymentNr, currentUser, out failedMessage))
                        return false;


                    context.SaveChanges();

                }

                else
                {
                    if (!mgr.TryScheduleDirectDebitChange(context, creditNr, b, paymentNr, customerId, paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro(), currentStatus, out failedMessage))
                        return false;

                    context.SaveChanges();
                }
            }

            failedMessage = null;
            return true;
        }

        public bool TryScheduleDirectDebitCancellation(string creditNr, string paymentNr, INTechCurrentUserMetadata currentUser, bool? isChangeActivated, out string failedMessage)
        {
            var mgr = directDebitBusinessEventManager;

            using (var context = contextFactory.CreateContext())
            {
                if (isChangeActivated != true)
                {
                    if (!TrySetPendingSchedulationKeyValueItem(context, InternalDirectDebitFileOperation.PendingCancellation, creditNr, null, null, paymentNr, currentUser, out failedMessage))
                        return false;

                    context.SaveChanges();
                }
                else
                {
                    if (!mgr.TryScheduleDirectDebitCancellation(context, creditNr, paymentNr, paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro(), out failedMessage))
                        return false;
                }

                context.SaveChanges();
            }

            failedMessage = null;
            return true;
        }

        public bool TryRemoveSchedulationDirectDebit(string creditNr, string paymentNr, INTechCurrentUserMetadata currentUser, out string failedMessage)
        {
            var mgr = directDebitBusinessEventManager;

            using (var context = contextFactory.CreateContext())
            {
                var directDebitPendingSchedulation = context.KeyValueItemsQueryable.Where(x => x.Key == creditNr && x.KeySpace == "InternalDirectDebitFileOperation").ToList();

                if (directDebitPendingSchedulation != null)
                {
                    directDebitPendingSchedulation.ForEach(pendingSchedulationItem => context.RemoveKeyValueItem(pendingSchedulationItem));

                    context.SaveChanges();
                }
            }

            failedMessage = null;
            return true;
        }

        public bool TryImportIncomingStatusFile(string fileName, byte[] fileBytes, string mimeType, bool? overrideDuplicateCheck, bool? overrideClientBgCheck, INTechCurrentUserMetadata user, out string failedMessage)
        {
            if (fileName == null)
            {
                failedMessage = $"Missing fileName";
                return false;
            }

            if (fileBytes == null)
            {
                failedMessage = $"Missing fileBytes";
                return false;
            }

            if (mimeType == null)
            {
                failedMessage = $"Missing mimeType";
                return false;
            }

            using (var context = contextFactory.CreateContext())
            {
                context.IsChangeTrackingEnabled = false;
                context.BeginTransaction();

                try
                {
                    var dc = documentClient;
                    Lazy<string> documentArchiveKey = new Lazy<string>(() => dc.ArchiveStore(fileBytes, mimeType, fileName));

                    using (var binaryDataAsStream = new MemoryStream(fileBytes))
                    {
                        var parser = new AutogiroMedgivandeAviseringFileParser();
                        var file = parser.Parse(binaryDataAsStream);

                        if (!overrideDuplicateCheck.HasValue || !overrideDuplicateCheck.Value)
                        {
                            if (context.IncomingDirectDebitStatusChangeFileHeadersQueryable.Any(x => x.Filename == fileName))
                            {
                                failedMessage = "File has already been imported. Override with overrideDuplicateCheck.";
                                return false;
                            }
                        }

                        if (!overrideClientBgCheck.GetValueOrDefault())
                        {
                            var expectedClientBgNr = paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro();
                            if (file.BankGiroNr.NormalizedValue != expectedClientBgNr.NormalizedValue || file.ResultItems.Any(y => y.BankGiroNr.NormalizedValue != expectedClientBgNr.NormalizedValue))
                            {
                                failedMessage = "File has statues to unexpected bankgiro accounts. Override with overrideClientBgCheck.";
                                return false;
                            }
                        }

                        var mgr = directDebitBusinessEventManager;
                        if (!mgr.TryImportIncomingDirectDebitStatusChangeFile(context, file, fileName, documentArchiveKey, out failedMessage))
                        {
                            return false;
                        }
                        context.DetectChanges();
                        context.SaveChanges();
                        context.CommitTransaction();
                        return true;
                    }
                }
                catch (AutogiroParserException ex)
                {
                    context.RollbackTransaction();
                    failedMessage = $"Invalid incoming direct debit status file: {ex.Message}";
                    return false;
                }
            }
        }
    }

    public class DirectDebitCreditDetails
    {
        public string CreditNr { get; set; }
        public List<ApplicantModel> Applicants { get; set; }
        public bool IsActive { get; set; }
        public SchedulationModel SchedulationChangesModel { get; set; }
        public DateTime CurrentIsActiveStateDate { get; set; }
        public int? AccountOwnerApplicantNr { get; set; }
        public BankAccountNrValidationResult BankAccount { get; set; }

        public class ApplicantModel
        {
            public int ApplicantNr { get; set; }
            public int CustomerId { get; set; }
            public string FirstName { get; set; }
            public DateTime? BirthDate { get; set; }
            public string StandardPaymentNr { get; set; }
        }

        public class SchedulationModel
        {
            public SchedulationDetailsModel SchedulationDetails { get; set; }
            public SchedulationDetailsModel PendingSchedulationDetails { get; set; }

        }

        public class SchedulationDetailsModel
        {
            public string SchedulationOperation { get; set; }
            public int? AccountOwnerApplicantNr { get; set; }
            public BankAccountNrValidationResult BankAccount { get; set; }

            public string PaymentNr { get; set; }
        }
    }

    public class DirectDebitEventModel
    {
        public int BusinessEventId { get; set; }
        public DateTime Date { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string LongText { get; set; }
    }

    public interface IDirectDebitService
    {
        Dictionary<int, int> FetchCustomerIdsByCreditNr(string creditNr);
        DirectDebitCreditDetails FetchCreditDetails(string creditNr, INTechCurrentUserMetadata currentUser, string backTarget = null);
        bool TryUpdateDirectDebitCheckStatusState(string creditNr, string newStatus, string bankAccountNr, int? bankAccountOwnerApplicantNr, INTechCurrentUserMetadata currentUser, out string failedMessage);
        List<DirectDebitEventModel> FetchEvents(string creditNr);
        bool TryScheduleDirectDebitActivation(string creditNr, string bankAccountNr, string paymentNr, int? customerId, INTechCurrentUserMetadata currentUser, bool? isChangeActivated, out string failedMessage);
        bool TryScheduleDirectDebitChange(string creditNr, string bankAccountNr, string paymentNr, int? customerId, INTechCurrentUserMetadata currentUser, bool? isChangeActivated, string currentStatus, out string failedMessage);

        bool TryScheduleDirectDebitCancellation(string creditNr, string paymentNr, INTechCurrentUserMetadata currentUser, bool? isChangeActivated, out string failedMessage);
        bool TryRemoveSchedulationDirectDebit(string creditNr, string paymentNr, INTechCurrentUserMetadata currentUser, out string failedMessage);
        bool TryImportIncomingStatusFile(string fileName, byte[] fileBytes, string mimeType, bool? overrideDuplicateCheck, bool? overrideClientBgCheck, INTechCurrentUserMetadata user, out string failedMessage);
    }
}