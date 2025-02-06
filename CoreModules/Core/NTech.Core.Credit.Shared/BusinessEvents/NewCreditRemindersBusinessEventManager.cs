using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewCreditRemindersBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly PaymentAccountService paymentAccountService;
        private readonly INotificationProcessSettingsFactory processSettingsFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ILoggingService loggingService;
        private readonly Func<IDocumentRenderer> createDocumentRenderer;
        private readonly PaymentOrderService paymentOrderService;

        public NewCreditRemindersBusinessEventManager(INTechCurrentUserMetadata currentUser, PaymentAccountService paymentAccountService, ICoreClock clock,
            IClientConfigurationCore clientConfiguration, INotificationProcessSettingsFactory processSettingsFactory, ICreditEnvSettings envSettings,
            CreditContextFactory creditContextFactory, ILoggingService loggingService, Func<IDocumentRenderer> createDocumentRenderer,
            PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.paymentAccountService = paymentAccountService;
            this.processSettingsFactory = processSettingsFactory;
            this.envSettings = envSettings;
            this.creditContextFactory = creditContextFactory;
            this.loggingService = loggingService;
            this.createDocumentRenderer = createDocumentRenderer;
            this.paymentOrderService = paymentOrderService;
        }

        public enum ReminderState
        {
            CannotBeReminded,
            HasCurrentNotDeliveredReminder,
            HasCurrentDeliveredReminder,
            CanBeReminded
        }

        public class ReminderStateItem
        {
            public CreditNotificationHeader Notification { get; set; }
            public int? NrOfDaysOverDue { get; set; }
            public int? NrOfDaysUntilReminderOverDue { get; set; }
            public int NrOfAdditionalRemindersAllowed { get; set; }
            public ReminderState State { get; set; }
        }

        public static List<ReminderStateItem> Reminders(ICreditContextExtended context, CreditType forCreditType, ReminderState? onlyForThisState, ICoreClock clock, INotificationProcessSettingsFactory processSettingsFactory, HashSet<string> onlyTheseCreditNrs = null)
        {
            var today = clock.Today;

            var processSettings = processSettingsFactory.GetByCreditType(forCreditType);

            var nPre = context
                .CreditNotificationHeadersQueryable
                .Where(x => x.Credit.Status == CreditStatus.Normal.ToString())
                .AsQueryable();

            if (onlyTheseCreditNrs != null && onlyTheseCreditNrs.Count > 0)
            {
                nPre = nPre.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
            }

            if (forCreditType == CreditType.MortgageLoan)
            {
                nPre = nPre.Where(x => x.Credit.CreditType == CreditType.MortgageLoan.ToString());
            }
            else
            {
                nPre = forCreditType == CreditType.CompanyLoan
                ? nPre.Where(x => x.Credit.CreditType == CreditType.CompanyLoan.ToString())
                : nPre.Where(x => x.Credit.CreditType != CreditType.MortgageLoan.ToString() && x.Credit.CreditType != CreditType.CompanyLoan.ToString());
            }

            var firstReminderDaysBefore = processSettings.FirstReminderDaysBefore ?? 0;
            var baseQuery = nPre
                .Select(x => new
                {
                    Notification = x,
                    IsOverDueAndUnpaid = !x.ClosedTransactionDate.HasValue && x.DueDate < today,
                    IsOnActiveCredit = x.Credit.Status == CreditStatus.Normal.ToString(),
                    CurrentReminderCount = x.Reminders.Count(),
                    HasActiveReminders = x.Reminders.Any(y => y.InternalDueDate > today),
                    HasActiveNotDeliveredReminders = x.Reminders.Any(y => y.InternalDueDate > today && !y.OutgoingCreditReminderDeliveryFileHeaderId.HasValue),
                    ReminderDueDate = x.Reminders.OrderByDescending(y => y.InternalDueDate).Select(y => (DateTime?)y.InternalDueDate).FirstOrDefault(),
                    IsCreditProcessSuspendedByTerminationLetter = x.Credit.TerminationLetters.Any(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null),
                    IsStandardDefaultProcessSuspended = x
                        .Credit
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.IsStandardDefaultProcessSuspended.ToString())
                        .OrderByDescending(y => y.Id)
                        .Select(y => y.Value)
                        .FirstOrDefault()
                })
                .ToList()
                .Select(x =>
                {
                    var nrOfDaysOverDue = (int)(today.Date - x.Notification.DueDate.Date).TotalDays;
                    var nrOfDaysUntilReminderOverDue = x.ReminderDueDate.HasValue
                        ? (int)(today.Date - x.ReminderDueDate.Value).TotalDays
                        : new int?();
                    return new ReminderStateItem
                    {
                        Notification = x.Notification,
                        NrOfDaysOverDue = nrOfDaysOverDue,
                        NrOfDaysUntilReminderOverDue = nrOfDaysUntilReminderOverDue,
                        NrOfAdditionalRemindersAllowed = processSettings.MaxNrOfReminders - x.CurrentReminderCount,
                        State = x.HasActiveNotDeliveredReminders
                        ? ReminderState.HasCurrentNotDeliveredReminder
                        : (x.HasActiveReminders
                            ? ReminderState.HasCurrentDeliveredReminder
                            : ((x.IsOverDueAndUnpaid && nrOfDaysOverDue >= firstReminderDaysBefore && x.IsOnActiveCredit && x.CurrentReminderCount < processSettings.MaxNrOfReminders)
                                ? !(x.IsCreditProcessSuspendedByTerminationLetter || x.IsStandardDefaultProcessSuspended == "true")
                                    ? ReminderState.CanBeReminded
                                    : ReminderState.CannotBeReminded
                                : ReminderState.CannotBeReminded))
                    };
                });

            return (onlyForThisState.HasValue
                ? baseQuery.Where(x => x.State == onlyForThisState.Value)
                : baseQuery).ToList();
        }

        public class CreditRemindersStatus
        {
            public int NotificationCountInMonth { get; set; }
            public int NrOfCurrentDeliveredReminders { get; set; }
            public int NrOfCurrentNotDeliveredReminders { get; set; }
            public int NrOfNotificationsPendingReminders { get; set; }
            public int NrOfRecentlyCreatedReminders { get; set; }
            public List<SkippedReminder> SkippedReminders { get; set; }

            public class SkippedReminder
            {
                public string CreditNr { get; set; }
                public DateTime NotificationDueDate { get; set; }
                public string SkippedReason { get; set; }
            }
        }

        public Dictionary<string, int> CreateReminders(ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType, HashSet<string> onlyTheseCreditNrs = null)
        {
            string GetDocumentPrefix(CreditType creditType)
            {
                switch (creditType)
                {
                    case CreditType.UnsecuredLoan: return "credit";
                    case CreditType.MortgageLoan: return "mortgageloan";
                    case CreditType.CompanyLoan: return "companyloan";
                    default: throw new NotImplementedException();
                }
            }
            using (var renderer = createDocumentRenderer())
            {
                Func<(IDictionary<string, object> PrintContext, string Filename, bool IsCoNotified), string> renderToArchive = x =>
                {
                    var templateName = $"{GetDocumentPrefix(forCreditType)}-{(x.IsCoNotified ? "co-" : "")}reminder";
                    return renderer.RenderDocumentToArchive(templateName, x.PrintContext, x.Filename);
                };    
                return CreateRemindersInternal(renderToArchive, customerPostalInfoRepository, forCreditType, onlyTheseCreditNrs: onlyTheseCreditNrs);
            }
        }

        private Dictionary<string, int> CreateRemindersInternal(Func<(IDictionary<string, object> PrintContext, string Filename, bool IsCoNotified), string> renderToArchive, ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType,
            HashSet<string> onlyTheseCreditNrs = null)
        {
            var reminderCountByCreditNr = new Dictionary<string, int>();

            using (var context = creditContextFactory.CreateContext())
            {
                var notificationIds = Reminders(context, forCreditType, ReminderState.CanBeReminded, Clock, processSettingsFactory, onlyTheseCreditNrs: onlyTheseCreditNrs).Select(x => x.Notification.Id).ToArray();

                var coNotificationResult = GroupForCoNotification(notificationIds, context);

                foreach (var notificationGroup in SplitIntoGroupsOfN(coNotificationResult.SingleNotifications, 100))
                {
                    RemindGroup(notificationGroup, context, renderToArchive, customerPostalInfoRepository, forCreditType, reminderCountByCreditNr, isCoNotifiedGroup: false);
                    context.SaveChanges(); //Intentionally save after each batch. The system recovers from this if some fail and others succeed
                }

                foreach(var coNotificationGroup in SplitIntoGroupsOfN(coNotificationResult.CoNotificationGroups, 50))
                {
                    RemindGroup(coNotificationGroup, context, renderToArchive, customerPostalInfoRepository, forCreditType, reminderCountByCreditNr, isCoNotifiedGroup: true);
                    context.SaveChanges(); //Intentionally save after each batch. The system recovers from this if some fail and others succeed
                }
            }

            return reminderCountByCreditNr;
        }

        private void RemindGroup(IEnumerable<List<NotificationToBeReminded>> notificationGroup, ICreditContextExtended context, 
            Func<(IDictionary<string, object> PrintContext, string Filename, bool IsCoNotified), string> renderToArchive, 
            ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType, Dictionary<string, int> reminderCountByCreditNr, bool isCoNotifiedGroup)
        {
            List<int> notificationIdGroup = notificationGroup.SelectMany(x => x.Select(y => y.NotificationId)).ToList();
            var models = CreditNotificationDomainModel.CreateForNotifications(notificationIdGroup, context, paymentOrderService.GetPaymentOrderItems());

            var maxReminderNumberByNotificationId = context
                .CreditReminderHeadersQueryable
                .Where(x => notificationIdGroup.Contains(x.NotificationId))
                .GroupBy(x => x.NotificationId)
                .Select(x => new
                {
                    x.Key,
                    MaxReminderNumber = x.Select(y => y.ReminderNumber).Max()
                }).ToDictionary(x => x.Key, x => x.MaxReminderNumber);

            var customerCards = GetApplicantCustomerInfoByNotificationIds(context, notificationIdGroup, customerPostalInfoRepository, forCreditType);
            var mortgageLoanInfoByNotificationId = GetMortgageLoanDataByNotificationId(context, notificationIdGroup, forCreditType);
            if(!isCoNotifiedGroup)
            {
                foreach (var notificationId in notificationIdGroup)
                {
                    var r = CreateReminder(context,
                        models[notificationId],
                        maxReminderNumberByNotificationId.ContainsKey(notificationId) ? new int?(maxReminderNumberByNotificationId[notificationId]) : null,
                        renderToArchive,
                        customerCards[notificationId],
                        forCreditType,
                        mortgageLoanInfoByNotificationId.Opt(notificationId),
                        null);
                    if (r != null)
                    {
                        reminderCountByCreditNr.AddOrUpdate(r.CreditNr, 1, x => x + 1);
                    }
                }
            }
            else
            {
                foreach(var coNotifiedReminders in notificationGroup)
                {
                    var coNotificationContext = new CoReminderContext(coNotifiedReminders);
                    //The master is done last so we have access to the print contexts from the previous ones
                    foreach (var notification in coNotificationContext.GetHandlingOrder())
                    {
                        var notificationId = notification.NotificationId;
                        var r = CreateReminder(context,
                            models[notificationId],
                            maxReminderNumberByNotificationId.ContainsKey(notificationId) ? new int?(maxReminderNumberByNotificationId[notificationId]) : null,
                            renderToArchive,
                            customerCards[notificationId],
                            forCreditType,
                            mortgageLoanInfoByNotificationId.Opt(notificationId),
                            coNotificationContext);
                        if (r != null)
                        {
                            reminderCountByCreditNr.AddOrUpdate(r.CreditNr, 1, x => x + 1);
                        }
                    }
                }
            }
        }

        private (List<NotificationToBeReminded>[] SingleNotifications, List<NotificationToBeReminded>[] CoNotificationGroups) GroupForCoNotification(int[] notificationIds, ICreditContextExtended context)
        {
            var singleNotifications = context.CreditNotificationHeadersQueryable
                .Where(x => notificationIds.Contains(x.Id) &&
                    //Notifications that were co-notified are candidates for co reminding. However, this requires some legal footing to send the entire group of loans to debt collection
                    //on late payments so we only co remind currently for loans that are part of a MortgageLoanAgreementNr which implies such a mandate. 
                    (x.CoNotificationId == null || !x.Credit.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString())))
                .Select(x => new NotificationToBeReminded { NotificationId = x.Id, ReminderNumber = x.Reminders.Count() + 1 })
                .ToArray();
            var singleNotificationIds = singleNotifications.Select(x => x.NotificationId).ToList();
            var candidateIds = notificationIds.Except(singleNotifications.Select(x => x.NotificationId).ToList());
            var coNotificationGroups = context
                .CreditNotificationHeadersQueryable
                .Where(x => candidateIds.Contains(x.Id))
                .GroupBy(x => x.CoNotificationId)
                .Select(x => x.Select(y => new NotificationToBeReminded
                    {
                        NotificationId = y.Id,
                        IsCoNotificationMaster = y.IsCoNotificationMaster,
                        ReminderNumber = y.Reminders.Count() + 1
                    }).ToList())
                .ToArray();
            foreach(var group in coNotificationGroups)
            {
                if(group.Count(x => x.IsCoNotificationMaster == true) != 1)
                {
                    //If the credit used as master for the notification has been settled or the master notification paid
                    ////we pick some random other credit to act as master here but we try to keep the same if possible.                    
                    foreach(var notification in group)
                    {
                        notification.IsCoNotificationMaster = false;
                    }
                    group[0].IsCoNotificationMaster = true;
                }
            }
            return (SingleNotifications: singleNotifications.Select(x => new List<NotificationToBeReminded> { x }).ToArray(), CoNotificationGroups: coNotificationGroups);
        }

        private class NotificationToBeReminded
        {
            public int NotificationId { get; set; }
            public int ReminderNumber { get; set; }
            public bool? IsCoNotificationMaster { get; set; }
        }

        private class MortgageLoanData
        {
            public string PropertyId { get; set; }
            public string MortgageLoanAgreementNr { get; set; }
            public string SharedOcrPaymentReference { get; set; }
        }

        private Dictionary<int, MortgageLoanData> GetMortgageLoanDataByNotificationId(ICreditContextExtended context, List<int> notificationIds, CreditType creditType)
        {
            if (ClientCfg.Country.BaseCountry != "SE" || creditType != CreditType.MortgageLoan)
                return new Dictionary<int, MortgageLoanData>();

            var notifications = context
                .CreditNotificationHeadersQueryable
                .Where(x => notificationIds.Contains(x.Id))
                .Select(x => new
                {
                    x.CreditNr,
                    MortgageLoanAgreementNr = x
                        .Credit
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString())
                        .OrderByDescending(y => y.Id)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    SharedOcrPaymentReference = x
                        .Credit
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString())
                        .OrderByDescending(y => y.Id)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    x.Id
                })
                .ToList();

            var propertyIdByCreditNr = MortgageLoanCollateralService.GetPropertyIdByCreditNr(context, notifications.Select(x => x.CreditNr).ToHashSetShared(), false);
            return notifications
                .ToDictionary(x => x.Id, x => new MortgageLoanData
                {
                    PropertyId = propertyIdByCreditNr?.Opt(x.CreditNr),
                    MortgageLoanAgreementNr = x.MortgageLoanAgreementNr,
                    SharedOcrPaymentReference = x.SharedOcrPaymentReference
                });
        }


        private CreditReminderHeader CreateReminder(ICreditContextExtended context, CreditNotificationDomainModel model, int? maxPreviousReminderNumber,
            Func<(IDictionary<string, object> PrintContext, string Filename, bool IsCoNotified), string> renderToArchive,
            List<ReminderReceiverCustomerModel> customerCards, CreditType creditType, MortgageLoanData mortgageLoanData, CoReminderContext coReminderContext)
        {
            var balance = model.GetRemainingBalance(Clock.Today);

            var processSettings = processSettingsFactory.GetByCreditType(creditType);
            var isCoReminderMaster = coReminderContext != null && coReminderContext.IsCoReminderMaster(model.NotificationId);

            if (balance <= processSettings.SkipReminderLimitAmount && !isCoReminderMaster)
            {
                loggingService.Information($"Reminder skipped on {model.NotificationId} due to {balance} < {processSettings.SkipReminderLimitAmount}");
                return null;
            }

            var evt = context.FillInfrastructureFields(new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.NewReminder.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date
            });
            context.AddBusinessEvent(evt);

            var internalDueDate = Clock.Today.AddDays(processSettings.ReminderMinDaysBetween);
            var reminder = context.FillInfrastructureFields(new CreditReminderHeader
            {
                BookKeepingDate = Now.ToLocalTime().Date,
                ReminderDate = Clock.Today,
                ReminderNumber = maxPreviousReminderNumber.HasValue ? maxPreviousReminderNumber.Value + 1 : 1,
                InternalDueDate = internalDueDate,
                TransactionDate = Now.ToLocalTime().Date,
                CreditNr = model.CreditNr,
                NotificationId = model.NotificationId,
                IsCoReminderMaster = coReminderContext == null ? new bool?() : (isCoReminderMaster ? true: false),
                CoReminderId = coReminderContext?.CoReminderId
            });
            context.AddCreditReminderHeaders(reminder);

            decimal newReminderFeeAmount = 0m;
            if (reminder.ReminderNumber > processSettings.NrOfFreeInitialReminders)
            {
                var nrOfPaidRemindersBefore = reminder.ReminderNumber - processSettings.NrOfFreeInitialReminders - 1;
                if ((!processSettings.MaxNrOfRemindersWithFees.HasValue || nrOfPaidRemindersBefore < processSettings.MaxNrOfRemindersWithFees.Value)
                    && (coReminderContext == null || isCoReminderMaster))
                {
                    newReminderFeeAmount = processSettings.ReminderFeeAmount;                    
                }
            }

            if (newReminderFeeAmount > 0m)
            {
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.ReminderFeeDebt,
                    newReminderFeeAmount,
                    evt.BookKeepingDate,
                    evt,
                    creditNr: model.CreditNr,
                    notificationId: model.NotificationId,
                    reminder: reminder));
            }

            var archiveKeys = new List<string>();
            //Create the pdf
            foreach (var customer in customerCards.OrderBy(x => x.ApplicantNr ?? x.CustomerId).ToList())
            {
                var applicantNr = customer.ApplicantNr;
                var customerId = customer.CustomerId;
                var customerCard = customer.PostalInfo;
                var totalCurrentAmount = (model.GetRemainingBalance(Clock.Today) + newReminderFeeAmount);
                var printContext = new ReminderPrintContext
                {
                    customerName = customerCard.GetCustomerName(),
                    companyName = customerCard.GetCompanyPropertyOrNull(x => x.CompanyName),
                    fullName = customerCard.GetPersonPropertyOrNull(x => x.FullName),
                    streetAddress = customerCard.StreetAddress,
                    areaAndZipcode = $"{customerCard.ZipCode} {customerCard.PostArea}",
                    ocrPaymentReference = new OcrNumberParser(ClientCfg.Country.BaseCountry).Parse(model.OcrPaymentReference).DisplayForm,
                    isReminder1 = reminder.ReminderNumber == 1 ? "true" : null,
                    newNotificationFeeAmount = newReminderFeeAmount <= 0m ? null : newReminderFeeAmount.ToString("C", PrintFormattingCulture),
                    notificationCurrentAmount = model.GetRemainingBalance(Clock.Today).ToString("C", PrintFormattingCulture),
                    notificationTotalAmount = model.GetInitialAmount(Clock.Today).ToString("C", PrintFormattingCulture),
                    totalCurrentAmount = totalCurrentAmount.ToString("C", PrintFormattingCulture),
                    notificationDueDate = model.DueDate.ToString("d", PrintFormattingCulture),
                    notificationMonth = model.DueDate.ToString(ClientCfg.Country.BaseCountry == "FI" ? "yyyy.MM" : "yyyy-MM"),
                    reminderDate = reminder.ReminderDate.ToString("d", PrintFormattingCulture),
                    reminderNumber = reminder.ReminderNumber.ToString(),
                    creditNr = reminder.CreditNr,
                    mortgageLoanPropertyId = mortgageLoanData?.PropertyId
                };

                var incomingPaymentBankAccountNr = paymentAccountService.GetIncomingPaymentBankAccountNr();
                if (ClientCfg.Country.BaseCountry == "FI")
                {
                    printContext.paymentIban = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr);
                }
                else
                {
                    printContext.paymentBankGiroNr = ClientCfg.Country.BaseCountry == "SE"
                        ? paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr)
                        : throw new NotImplementedException();
                }

                if(coReminderContext == null)
                {
                    //Normal single reminder
                    var receiverSuffix = applicantNr.HasValue ? applicantNr.Value.ToString() : $"c{customerId}";
                    var archiveKey = renderToArchive((
                            JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(printContext)),
                            $"creditreminder_{model.CreditNr}_{model.DueDate:yyyy-MM-dd}_{reminder.ReminderNumber}_{receiverSuffix}.pdf", false));

                    AddCreditDocument("Reminder" + reminder.ReminderNumber, applicantNr, archiveKey, context, reminder: reminder, customerId: customerId);
                    archiveKeys.Add(archiveKey);
                }
                else 
                {
                    //Co notification reminder. We store the print context so that when the last one in the group which is the master shows up we have access to all the print contexts
                    coReminderContext.AddReminder(printContext, customer, totalCurrentAmount);
                }

                if(isCoReminderMaster)
                {
                    if (mortgageLoanData == null)//To relax this, figure out a way to thread shared things like shared ocr into here without piggybacking on mortgageLoanData.
                        throw new Exception("Co notification for reminders currently only supported for mortgage loans.");
                    //Co notification master
                    var reminderPrintContexts = coReminderContext.ReminderPrintContexts(customer.CustomerId);
                    var anySingleContext = reminderPrintContexts.Last();
                    //The agreement nr concept implies that the most overdue loan sets the condition for the entire agreement so the most harsh threat applies to all of them so that is what we use
                    var sharedReminderNumber = coReminderContext.Notifications.Max(x => x.ReminderNumber); 
                    var masterPrintContext = new
                    {
                        anySingleContext.customerName,
                        anySingleContext.fullName,
                        anySingleContext.companyName,
                        anySingleContext.streetAddress,
                        anySingleContext.areaAndZipcode,
                        anySingleContext.reminderDate,
                        anySingleContext.paymentBankGiroNr,
                        anySingleContext.paymentIban,
                        anySingleContext.mortgageLoanPropertyId,
                        isReminder1 = sharedReminderNumber == 1 ? "true" : null,
                        reminderNumber = sharedReminderNumber,
                        mortgageLoanAgreementNr = mortgageLoanData.MortgageLoanAgreementNr,
                        sharedOcrPaymentReference = mortgageLoanData.SharedOcrPaymentReference,
                        sharedTotalCurrentAmount = coReminderContext.SharedTotalCurrentAmount(customer.CustomerId).ToString("C", PrintFormattingCulture),
                        reminders = reminderPrintContexts
                    };
                    var receiverSuffix = applicantNr.HasValue ? applicantNr.Value.ToString() : $"c{customerId}";
                    var archiveKey = renderToArchive((
                            JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(masterPrintContext)),
                            $"creditreminder_{model.CreditNr}_{model.DueDate:yyyy-MM-dd}_{reminder.ReminderNumber}_{receiverSuffix}.pdf", true));

                    AddCreditDocument("Reminder" + reminder.ReminderNumber, applicantNr, archiveKey, context, reminder: reminder, customerId: customerId);
                    
                    archiveKeys.Add(archiveKey);
                }
            }
            var comment = $"Reminder {reminder.ReminderNumber} created for the notification with Due date: {model.DueDate.ToString("d", CommentFormattingCulture)}. Reminder fee of {newReminderFeeAmount.ToString("f2", CommentFormattingCulture)} added.";

            //TODO: How does the user keep track of the co notified reminders?

            AddComment(
                $"Reminder {reminder.ReminderNumber} created for the notification with Due date: {model.DueDate.ToString("d", CommentFormattingCulture)}. Reminder fee of {newReminderFeeAmount.ToString("f2", CommentFormattingCulture)} added.",
                BusinessEventType.NewReminder,
                context,
                creditNr: model.CreditNr,
                attachment: archiveKeys.Count > 0 ? CreditCommentAttachmentModel.ArchiveKeysOnly(archiveKeys): null);

            return reminder;
        }

        private class CoReminderContext
        {
            private readonly List<NotificationToBeReminded> notifications;
            public List<NotificationToBeReminded> Notifications => notifications;
            public string CoReminderId { get; set; } = Guid.NewGuid().ToString();
            
            private Dictionary<int, CustomerDataModel> PerCustomerData = new Dictionary<int, CustomerDataModel>();

            public CoReminderContext(List<NotificationToBeReminded> notifications)
            {
                this.notifications = notifications;
            }

            //Make sure the master is handled last so it has access to the data and print context for all.
            public IEnumerable<NotificationToBeReminded> GetHandlingOrder() =>
                notifications.OrderBy(x => x.IsCoNotificationMaster == true ? 1 : 0);

            public bool IsCoReminderMaster(int notificationId) => 
                notifications.Single(x => x.NotificationId == notificationId).IsCoNotificationMaster == true;

            public void AddReminder(ReminderPrintContext printContext, ReminderReceiverCustomerModel customer, decimal totalCurrentAmount)
            {
                if (!PerCustomerData.ContainsKey(customer.CustomerId))
                    PerCustomerData[customer.CustomerId] = new CustomerDataModel();

                var customerData = PerCustomerData[customer.CustomerId];
                customerData.PrintContexts.Add(printContext);
                customerData.SharedTotalCurrentAmount += totalCurrentAmount;
            }

            public List<ReminderPrintContext> ReminderPrintContexts(int customerId) => PerCustomerData[customerId].PrintContexts;
            public decimal SharedTotalCurrentAmount(int customerId) => PerCustomerData[customerId].SharedTotalCurrentAmount;

            private class CustomerDataModel 
            {
                public List<ReminderPrintContext> PrintContexts = new List<ReminderPrintContext>();
                public decimal SharedTotalCurrentAmount = 0m;
            }
        }

        public OutgoingCreditReminderDeliveryFileHeader CreateDeliveryExport(List<string> errors, IDocumentClient documentClient, ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var dPre = context
                    .CreditReminderHeadersQueryable
                    .Where(x => !x.OutgoingCreditReminderDeliveryFileHeaderId.HasValue);

                if (forCreditType == CreditType.MortgageLoan)
                {
                    dPre = dPre.Where(x => x.Credit.CreditType == CreditType.MortgageLoan.ToString());
                }
                else
                {
                    dPre = forCreditType == CreditType.CompanyLoan
                    ? dPre.Where(x => x.Credit.CreditType == CreditType.CompanyLoan.ToString())
                    : dPre.Where(x => x.Credit.CreditType != CreditType.MortgageLoan.ToString() && x.Credit.CreditType != CreditType.CompanyLoan.ToString());
                }

                var ns = dPre
                    .Select(x => new
                    {
                        Reminder = x,
                        x.Notification,
                        Customers = x.Credit.CreditCustomers,
                        x.Documents
                    })
                    .ToList();

                if (ns.Count == 0)
                {
                    return null;
                }

                customerPostalInfoRepository.PreFetchCustomerPostalInfo(new HashSet<int>(ns.SelectMany(x => x.Customers.Select(y => y.CustomerId))));

                var f = new OutgoingCreditReminderDeliveryFileHeader
                {
                    ChangedById = UserId,
                    ExternalId = Guid.NewGuid().ToString(),
                    InformationMetaData = InformationMetadata,
                    TransactionDate = Clock.Today,
                    ChangedDate = Clock.Now
                };

                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var tempZipfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                Directory.CreateDirectory(tempFolder);
                try
                {
                    List<XElement> meta = new List<XElement>();
                    foreach (var n in ns)
                    {
                        var header = n.Reminder;
                        var notification = n.Notification;
                        var customerIdByApplicantNr = n.Customers.ToDictionary(x => x.ApplicantNr, x => x.CustomerId);

                        if (n.Documents == null || !n.Documents.Any())
                        {
                            errors.Add("Missing pdfs for credit " + header.CreditNr);
                        }
                        else
                        {
                            foreach (var d in n.Documents)
                            {
                                var customerId = d.CustomerId ?? customerIdByApplicantNr[d.ApplicantNr.Value];
                                var postalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(customerId);

                                var (IsSuccess, ContentType, FileName, FileData) = documentClient.TryFetchRaw(d.ArchiveKey);
                                if (!IsSuccess)
                                {
                                    throw new Exception($"Missing document {d.ArchiveKey}");
                                }

                                var fileName = $"creditreminder_{header.CreditNr}_{notification.DueDate:yyyy-MM-dd}_{header.ReminderNumber}_{(d.ApplicantNr.HasValue ? d.ApplicantNr.Value.ToString() : $"c{d.CustomerId.Value}")}.pdf";
                                System.IO.File.WriteAllBytes(Path.Combine(tempFolder, fileName), FileData);
                                meta.Add(new XElement("CreditReminder",
                                    new XElement("CreditNr", header.CreditNr),
                                    new XElement("ApplicantNr", d.ApplicantNr),
                                    new XElement("CustomerId", d.CustomerId),
                                    new XElement("Name", postalInfo.GetCustomerName()),
                                    new XElement("Street", postalInfo.StreetAddress),
                                    new XElement("City", postalInfo.PostArea),
                                    new XElement("Zip", postalInfo.ZipCode),
                                    new XElement("Country", postalInfo.AddressCountry ?? ClientCfg.Country.BaseCountry),
                                    new XElement("PdfFileName", fileName)
                                    ));
                            }
                            header.DeliveryFile = f;
                        }
                    }

                    XDocument metaDoc = new XDocument(new XElement("CreditReminders",
                        new XAttribute("creationDate", Clock.Now.ToString("o")),
                        new XAttribute("deliveryId", f.ExternalId),
                        meta));

                    metaDoc.Save(Path.Combine(tempFolder, "creditreminder_metadata.xml"));

                    var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();

                    fs.CreateZip(tempZipfile, tempFolder, true, null);

                    var filename = $"creditreminder_{Clock.Today:yyyy-MM}_{f.ExternalId}.zip";
                    f.FileArchiveKey = documentClient.ArchiveStoreFile(
                        new FileInfo(tempZipfile),
                        "application/zip",
                        filename);

                    context.SaveChanges();

                    if (envSettings.OutgoingCreditNotificationDeliveryFolder != null)
                    {
                        var targetFolder = envSettings.OutgoingCreditNotificationDeliveryFolder;
                        targetFolder.Create();
                        System.IO.File.Copy(tempZipfile, Path.Combine(targetFolder.FullName, filename));
                    }

                    if (envSettings.CreditRemindersExportProfileName != null)
                    {
                        documentClient.TryExportArchiveFile(f.FileArchiveKey, envSettings.CreditRemindersExportProfileName, filename: filename);
                    }

                    return f;
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempFolder, true);
                        if (System.IO.File.Exists(tempZipfile))
                        {
                            System.IO.File.Delete(tempZipfile);
                        }
                    }
                    catch { /* ignored*/ }
                }
            }
        }

        private class ReminderPrintContext
        {
            public string creditNr { get; set; }
            public string customerName { get; set; }
            public string fullName { get; set; }
            public string companyName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string reminderDate { get; set; }
            public string paymentIban { get; set; }
            public string ocrPaymentReference { get; set; }
            public string reminderNumber { get; set; }
            public string isReminder1 { get; set; }
            public string notificationMonth { get; set; }
            public string notificationDueDate { get; set; }
            public string notificationTotalAmount { get; set; }
            public string notificationCurrentAmount { get; set; }
            public string newNotificationFeeAmount { get; set; }
            public string totalCurrentAmount { get; set; }
            public string paymentBankGiroNr { get; set; }
            public string mortgageLoanPropertyId { get; set; }
        }

        public CreditRemindersStatus GetStatus(ICreditContextExtended context, CreditType forCreditType)
        {
            var potentialReminders = Reminders(context, forCreditType, null, Clock, processSettingsFactory);

            Dictionary<ReminderState, int> countsByState;
            List<CreditRemindersStatus.SkippedReminder> skippedReminders = null;

            var oneWeekAgo = Clock.Today.AddDays(-7);
            var recentRemindersCount = context.CreditReminderHeadersQueryable.Count(x => x.TransactionDate > oneWeekAgo);

            var currentYear = Clock.Today.Year;
            var currentMonth = Clock.Today.Month;
            int nrOfCurrentDeliveredReminders;
            int nrOfCurrentNotDeliveredReminders;

            Dictionary<ReminderState, int> GetCountsByState(List<ReminderStateItem> reminders)
            {
                return reminders
                    .GroupBy(x => x.State)
                    .Select(x => new
                    {
                        State = x.Key,
                        Count = x.Count()
                    })
                    .ToDictionary(x => x.State, x => x.Count);
            }

            if (envSettings.HasPerLoanDueDay)
            {
                countsByState = GetCountsByState(potentialReminders);
                nrOfCurrentDeliveredReminders = potentialReminders.Count(x => x.State == ReminderState.HasCurrentDeliveredReminder && x.NrOfDaysUntilReminderOverDue.HasValue && x.NrOfDaysUntilReminderOverDue.Value > 7);
                nrOfCurrentNotDeliveredReminders = potentialReminders.Count(x => x.State == ReminderState.HasCurrentNotDeliveredReminder && x.NrOfDaysUntilReminderOverDue.HasValue && x.NrOfDaysUntilReminderOverDue.Value > 7);
                skippedReminders = GetPerLoanDueDateSkipReasons(potentialReminders);
            }
            else
            {
                var (skippedReasons, remainingPotentialReminders) = SplitOutSkippedRemindersForCreditsWithMonthlyDueDate(potentialReminders, forCreditType, Clock.Today);
                countsByState = GetCountsByState(remainingPotentialReminders);
                nrOfCurrentDeliveredReminders = countsByState.ContainsKey(ReminderState.HasCurrentDeliveredReminder) ? countsByState[ReminderState.HasCurrentDeliveredReminder] : 0;
                nrOfCurrentNotDeliveredReminders = countsByState.ContainsKey(ReminderState.HasCurrentNotDeliveredReminder) ? countsByState[ReminderState.HasCurrentNotDeliveredReminder] : 0;
                skippedReminders = skippedReasons;
            }

            return new CreditRemindersStatus
            {
                NotificationCountInMonth = context.CreditNotificationHeadersQueryable.Where(x => x.DueDate.Year == currentYear && x.DueDate.Month == currentMonth).Count(),
                NrOfCurrentDeliveredReminders = nrOfCurrentDeliveredReminders,
                NrOfCurrentNotDeliveredReminders = nrOfCurrentNotDeliveredReminders,
                NrOfNotificationsPendingReminders = countsByState.ContainsKey(ReminderState.CanBeReminded) ? countsByState[ReminderState.CanBeReminded] : 0,
                NrOfRecentlyCreatedReminders = recentRemindersCount,
                SkippedReminders = skippedReminders
            };
        }

        private List<CreditRemindersStatus.SkippedReminder> GetPerLoanDueDateSkipReasons(List<ReminderStateItem> potentialReminders)
        {
            return potentialReminders.Where(x => x.State != ReminderState.CanBeReminded && !(x.State == ReminderState.CannotBeReminded && x.NrOfAdditionalRemindersAllowed == 0 && x.NrOfDaysUntilReminderOverDue.HasValue && x.NrOfDaysUntilReminderOverDue.Value < 0)).Select(x =>
            {
                var skipReason = $"{x.State}: ";
                if (x.NrOfDaysUntilReminderOverDue.HasValue)
                {
                    if (x.NrOfDaysUntilReminderOverDue.Value > 0)
                    {
                        skipReason += $"Current reminder overdue in {x.NrOfDaysUntilReminderOverDue} days";
                    }
                    else
                    {
                        skipReason += $"Current reminder overdue";
                    }
                }
                else if (x.NrOfDaysOverDue.HasValue && x.NrOfDaysOverDue > 0)
                {
                    skipReason += $"Notification overdue by {x.NrOfDaysOverDue} days";
                }
                else
                {
                    skipReason += $"Notification not overdue until {x.Notification.DueDate:yyyy-MM-dd}";
                }

                if (x.State == ReminderState.HasCurrentDeliveredReminder || x.State == ReminderState.HasCurrentNotDeliveredReminder || x.State == ReminderState.CannotBeReminded)
                {
                    skipReason += $", {x.NrOfAdditionalRemindersAllowed} additional reminders allowed";
                }

                return new CreditRemindersStatus.SkippedReminder
                {
                    CreditNr = x.Notification.CreditNr,
                    NotificationDueDate = x.Notification.DueDate,
                    SkippedReason = skipReason
                };
            }).ToList();
        }

        private (List<CreditRemindersStatus.SkippedReminder> SkippedReasons, List<ReminderStateItem> RemainingPotentialReminders) SplitOutSkippedRemindersForCreditsWithMonthlyDueDate(List<ReminderStateItem> potentialReminders, CreditType forCreditType, DateTime today)
        {
            var skippedReasons = new List<CreditRemindersStatus.SkippedReminder>();
            var remainingPotentialReminders = new List<ReminderStateItem>(potentialReminders.Count);
            var skippedNotificationIds = new HashSet<int>();

            var processSettings = processSettingsFactory.GetByCreditType(forCreditType);
            var skipReminderLimitAmount = processSettings.SkipReminderLimitAmount;
            var canBeReminded = potentialReminders.Where(x => x.State == ReminderState.CanBeReminded);

            //NOTE: In theory this would be better in the Reminders method as part of the linq query
            //      but in practice that turns out to be too slow
            foreach (var notificationIdGroup in canBeReminded.Select(x => x.Notification.Id).ToArray().SplitIntoGroupsOfN(250))
            {
                using (var context = creditContextFactory.CreateContext())
                {
                    var notificationSkippedForLowBalance = CurrentNotificationStateReminder
                        .GetCurrentOpenNotificationsStateQuery(context)
                        .Where(x => notificationIdGroup.Contains(x.NotificationId) && x.RemainingAmount <= skipReminderLimitAmount)
                        .Select(x => new
                        {
                            x.NotificationId,
                            x.DueDate,
                            x.CreditNr,
                            x.RemainingAmount
                        })
                        .ToList();
                    foreach (var n in notificationSkippedForLowBalance)
                    {
                        skippedReasons.Add(new CreditRemindersStatus.SkippedReminder
                        {
                            CreditNr = n.CreditNr,
                            NotificationDueDate = n.DueDate,
                            SkippedReason = $"Notification balance {n.RemainingAmount} <= {skipReminderLimitAmount}"
                        });
                        skippedNotificationIds.Add(n.NotificationId);
                    }
                }
            }

            //We do this in a second step to make sure we preserve the original order
            foreach (var potentialReminder in potentialReminders)
            {
                if (!skippedNotificationIds.Contains(potentialReminder.Notification.Id))
                {
                    remainingPotentialReminders.Add(potentialReminder);
                }
            }

            return (SkippedReasons: skippedReasons, RemainingPotentialReminders: remainingPotentialReminders);
        }

        public class ReminderReceiverCustomerModel
        {
            public int NotificationId { get; set; }
            public int? ApplicantNr { get; set; }
            public int CustomerId { get; set; }
            public bool IsCompany { get; set; }
            public SharedCustomerPostalInfo PostalInfo { get; set; }
        }

        private IDictionary<int, List<ReminderReceiverCustomerModel>> GetApplicantCustomerInfoByNotificationIds(ICreditContextExtended context, List<int> notificationIds, ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType)
        {
            var all = new List<ReminderReceiverCustomerModel>();

            all.AddRange(context
                .CreditNotificationHeadersQueryable.Where(x => notificationIds.Contains(x.Id))
                .SelectMany(x => x.Credit.CreditCustomers.Select(y => new ReminderReceiverCustomerModel
                {
                    CustomerId = y.CustomerId,
                    ApplicantNr = y.ApplicantNr,
                    NotificationId = x.Id,
                    IsCompany = x.Credit.CreditType == CreditType.CompanyLoan.ToString()
                }))
                .ToList());

            if (forCreditType == CreditType.CompanyLoan)
            {
                var companyLoanCollateralCustomers = context
                    .CreditNotificationHeadersQueryable.Where(x => notificationIds.Contains(x.Id))
                    .SelectMany(x => x.Credit.CustomerListMembers.Where(y => y.ListName == "companyLoanCollateral").Select(y => new { y.CustomerId, x.Id }))
                    .ToList()
                    .Select(x => new ReminderReceiverCustomerModel
                    {
                        ApplicantNr = null,
                        CustomerId = x.CustomerId,
                        NotificationId = x.Id,
                        IsCompany = false
                    })
                    .ToList();

                all.AddRange(companyLoanCollateralCustomers);
            }
            else if (forCreditType == CreditType.MortgageLoan)
            {
                var notificationsWithCollateral = context
                    .CreditNotificationHeadersQueryable.Where(x => notificationIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        ApplicantCustomerIds = x.Credit.CreditCustomers.Select(y => y.CustomerId),
                        CollateralModelRaw = context
                            .KeyValueItemsQueryable
                            .Where(y => y.KeySpace == KeyValueStoreKeySpaceCode.MortgageLoanCollateralsV1.ToString() && y.Key == x.CreditNr)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                    }).ToList();
                foreach (var notification in notificationsWithCollateral.Where(x => x.CollateralModelRaw != null))
                {
                    var m = JsonConvert.DeserializeObject<MortgageLoanCollateralsModel>(notification.CollateralModelRaw);
                    var additionalCustomers = new Lazy<HashSet<int>>(() => new HashSet<int>());
                    foreach (var c in m.Collaterals.Where(x => x.CustomerIds != null))
                    {
                        foreach (var customerId in c.CustomerIds.Except(notification.ApplicantCustomerIds))
                        {
                            additionalCustomers.Value.Add(customerId);
                        }
                    }
                    if (additionalCustomers.IsValueCreated)
                    {
                        all.AddRange(additionalCustomers.Value.Select(customerId => new ReminderReceiverCustomerModel
                        {
                            ApplicantNr = null,
                            CustomerId = customerId,
                            IsCompany = false,
                            NotificationId = notification.Id
                        }));
                    }
                }
            }

            customerPostalInfoRepository.PreFetchCustomerPostalInfo(new HashSet<int>(all.Select(x => x.CustomerId)));

            var d = new Dictionary<int, List<ReminderReceiverCustomerModel>>();

            foreach (var notification in all.GroupBy(x => x.NotificationId).Select(x => new { NotificationId = x.Key, Customers = x }))
            {
                foreach (var customer in notification.Customers)
                {
                    var customerId = customer.CustomerId;
                    var applicantNr = customer.ApplicantNr;
                    customer.PostalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(customerId);

                    if (!d.ContainsKey(notification.NotificationId))
                    {
                        d[notification.NotificationId] = new List<ReminderReceiverCustomerModel>();
                    }

                    d[notification.NotificationId].Add(customer);
                }
            }

            return d;
        }

        private class CurrentNotificationStateReminder
        {
            public decimal RemainingAmount { get; set; }
            public string CreditNr { get; set; }
            public int NotificationId { get; set; }
            public DateTime DueDate { get; set; }

            public static IQueryable<CurrentNotificationStateReminder> GetCurrentOpenNotificationsStateQuery(ICreditContextExtended context)
            {
                return context
                    .CreditNotificationHeadersQueryable
                    .Where(x => !x.ClosedTransactionDate.HasValue)
                    .Select(x => new
                    {
                        NotificationId = x.Id,
                        x.CreditNr,
                        x.DueDate,
                        InitialCapitalAmount = -x.Transactions.Where(y => y.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() && y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        InitialNonCapitalAmount = x.Transactions.Where(y => (y.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() || y.BusinessEvent.EventType == BusinessEventType.NewReminder.ToString()) && y.AccountCode != TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        PaidCapitalAmount = -x.Transactions.Where(y => y.IncomingPaymentId.HasValue && y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        PaidNonCapitalAmount = -x.Transactions.Where(y => y.IncomingPaymentId.HasValue && y.AccountCode != TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        WrittenOffCapitalAmount = x.Transactions.Where(y => y.WriteoffId.HasValue && y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        WrittenOffNonCapitalAmount = -x.Transactions.Where(y => y.WriteoffId.HasValue && y.AccountCode != TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    })
                    .Select(x => new
                    {
                        x.NotificationId,
                        x.CreditNr,
                        x.DueDate,
                        InitialAmount = x.InitialNonCapitalAmount + x.InitialCapitalAmount,
                        PaidAmount = x.PaidCapitalAmount + x.PaidNonCapitalAmount,
                        WrittenOffAmount = x.WrittenOffCapitalAmount + x.WrittenOffNonCapitalAmount
                    })
                    .Select(x => new CurrentNotificationStateReminder
                    {
                        NotificationId = x.NotificationId,
                        CreditNr = x.CreditNr,
                        DueDate = x.DueDate,
                        RemainingAmount = x.InitialAmount - x.PaidAmount - x.WrittenOffAmount,
                    });
            }
        }
    }
}