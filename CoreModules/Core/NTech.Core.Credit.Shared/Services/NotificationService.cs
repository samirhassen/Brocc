using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nCredit.Code.Services
{
    public class NotificationService : UngroupedNotificationService
    {
        private readonly ICoreClock clock;
        private readonly ISnailMailLoanDeliveryService snailmailDeliveryService;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ILoggingService loggingService;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICustomerClient customerClient;
        private readonly INotificationDocumentBatchRenderer renderer;
        private readonly AlternatePaymentPlanService paymentPlanService;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly PaymentOrderService paymentOrderService;

        public NotificationService(ICoreClock clock, ISnailMailLoanDeliveryService snailmailDeliveryService,
            PaymentAccountService paymentAccountService, CreditContextFactory creditContextFactory, ILoggingService loggingService,
            ICreditEnvSettings envSettings, IClientConfigurationCore clientConfiguration, INotificationProcessSettingsFactory notificationProcessSettingsFactory,
            ICustomerClient customerClient, INotificationDocumentBatchRenderer renderer, AlternatePaymentPlanService paymentPlanService, INTechCurrentUserMetadata currentUser,
            PaymentOrderService paymentOrderService)
        {
            this.clock = clock;
            this.snailmailDeliveryService = snailmailDeliveryService;
            this.paymentAccountService = paymentAccountService;
            this.creditContextFactory = creditContextFactory;
            this.loggingService = loggingService;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.customerClient = customerClient;
            this.renderer = renderer;
            this.paymentPlanService = paymentPlanService;
            this.currentUser = currentUser;
            this.paymentOrderService = paymentOrderService;
        }

        public CreateNotificationsResult CreateNotifications(bool skipDeliveryExport, bool skipNotify, List<string> onlyTheseCreditNrs = null)
        {
            return renderer.WithRenderer(r => CreateNotifications(r, skipDeliveryExport, skipNotify, onlyTheseCreditNrs: onlyTheseCreditNrs));
        }

        public List<CreditNotificationGroup> GetCreditGroupsToNotifyComposable(
            ICreditContextExtended context,
            Action<Dictionary<string, string>> observeSkipReasonsByCreditNr = null)
        {
            var notificationSettings = notificationProcessSettingsFactory.GetByCreditType(envSettings.ClientCreditType);
            int? fixedNotificationDueDay = envSettings.HasPerLoanDueDay ? new int?() : notificationSettings.NotificationDueDay;

            var r = GetCreditGroupsToNotify(context, fixedNotificationDueDay, out var skippedCreditNrsWithReasons);

            observeSkipReasonsByCreditNr?.Invoke(skippedCreditNrsWithReasons);

            return r;
        }

        private List<CreditNotificationGroup> GetCreditGroupsToNotify(
            ICreditContextExtended context,
            int? fixedDueDay,
            out Dictionary<string, string> skippedCreditNrsWithReasons,
            List<string> onlyTheseCreditNrs = null)
        {
            var today = context.CoreClock.Today;
            var isCoNotificationEnabled = CreditFeatureToggles.IsCoNotificationEnabled(clientConfiguration);

            var creditsPre = context.CreditHeadersQueryable;
            var creditType = envSettings.ClientCreditType;

            if (creditType == CreditType.MortgageLoan)
            {
                creditsPre = creditsPre.Where(x => x.CreditType == CreditType.MortgageLoan.ToString());
            }
            else if (creditType == CreditType.CompanyLoan)
            {
                creditsPre = creditsPre.Where(x => x.CreditType == CreditType.CompanyLoan.ToString());
            }
            else
            {
                var otherTypes = new List<string> { CreditType.MortgageLoan.ToString(), CreditType.CompanyLoan.ToString() };
                creditsPre = creditsPre.Where(x => !otherTypes.Contains(x.CreditType));
            }

            if (onlyTheseCreditNrs != null)
            {
                creditsPre = creditsPre.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
            }

            var currentYear = today.Year;
            var currentMonth = today.Month;

            var items = creditsPre
                .Where(x => x.Status == CreditStatus.Normal.ToString())
                .Select(x => new
                {
                    Common = new CreditNotificationStatusCommon
                    {
                        CreditNr = x.CreditNr,
                        IsMissingNotNotifiedCapital = (x.Transactions.Where(y => y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m) <= 0m,
                        CreditStartDate = x.CreatedByEvent.TransactionDate,
                        Applicant1CustomerId = x.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => (int?)y.CustomerId).FirstOrDefault(),
                        CreditStatus = x.Status,
                        SinglePaymentLoanRepaymentDays = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.SinglePaymentLoanRepaymentDays.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => (int?)y.Value)
                            .FirstOrDefault(),
                        LatestNotificationDueDate = x.Notifications.OrderByDescending(y => y.DueDate).Select(y => (DateTime?)y.DueDate).FirstOrDefault(),
                        LatestPaymentFreeMonthDueDate = x.CreditPaymentFreeMonths.OrderByDescending(y => y.DueDate).Select(y => (DateTime?)y.DueDate).FirstOrDefault(),
                        PerLoanDueDay = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.NotificationDueDay.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => (int?)y.Value)
                            .FirstOrDefault(),
                        IsCreditProcessSuspendedByTerminationLetter = x.TerminationLetters.Any(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null),
                        IsStandardDefaultProcessSuspended = (x
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.IsStandardDefaultProcessSuspended.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.Value)
                            .FirstOrDefault() == "true")
                    },
                    SharedOcrPaymentReference = x
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    IsPreferredCoNotificationMaster = !x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.MainCreditCreditNr.ToString())
                })
                .ToList();

            var skippedCreditNrsWithReasonsLocal = new Dictionary<string, string>();
            var candidates = items.Select(x =>
            {
                var (isNotified, dueDate, skipReason) = GetNotificationDueDateOrSkipReason(x.Common, today, fixedDueDay);
                if (isNotified)
                {
                    return x;
                }
                else
                {
                    skippedCreditNrsWithReasonsLocal[x.Common.CreditNr] = skipReason;
                    return null;
                }
            }).Where(x => x != null).ToList();

            skippedCreditNrsWithReasons = skippedCreditNrsWithReasonsLocal;

            var result = new List<CreditNotificationGroup>();
            foreach (var candidateGroup in candidates.GroupBy(x => x.SharedOcrPaymentReference).ToList())
            {
                var sharedOcrPaymentReference = candidateGroup.Key;

                if (!isCoNotificationEnabled || sharedOcrPaymentReference == null || candidateGroup.Count() == 1 || candidateGroup.Select(x => x.Common.PerLoanDueDay ?? -1).Distinct().Count() > 1)
                {
                    //The feature for co-notification must be enabled
                    //No shared payment reference or only a single notification (others fully paid, already notified or not created yet)
                    //Different due dates cannot be co notified
                    result.AddRange(candidateGroup.Select(x => new CreditNotificationGroup { Common = x.Common, CoNotificationId = null, CoNotifiedCredits = new List<CreditNotificationStatusCommon>() }));
                }
                else
                {
                    //If there is no preferred master just use any of the credits in the group
                    var master = candidateGroup.FirstOrDefault(x => x.IsPreferredCoNotificationMaster) ?? candidateGroup.First();
                    result.Add(new CreditNotificationGroup
                    {
                        Common = master.Common,
                        CoNotificationId = Guid.NewGuid().ToString(),
                        CoNotifiedCredits = candidateGroup.Where(x => x.Common.CreditNr != master.Common.CreditNr).Select(x => x.Common).ToList()
                    });
                }
            }

            return result;
        }

        public class CreditNotificationGroup
        {
            public CreditNotificationStatusCommon Common { get; set; }
            public string CoNotificationId { get; set; }
            public List<CreditNotificationStatusCommon> CoNotifiedCredits { get; set; }
        }

        private CreateNotificationsResult CreateNotifications(INotificationDocumentRenderer renderer, bool skipDeliveryExport, bool skipNotify,
            List<string> onlyTheseCreditNrs = null)
        {
            CancelDefaultedOrCompleteFullyPaidPaymentsPlans(onlyTheseCreditNrs: onlyTheseCreditNrs);

            var creditType = envSettings.ClientCreditType;
            var isForMortgageLoans = creditType == CreditType.MortgageLoan;
            var isForCompanyLoans = creditType == CreditType.CompanyLoan;

            var resultByCreditNr = new Dictionary<string, (string CreditNr, NotificationResultCode Result, string ErrorMessage)>();
            void ObserveNotificationResult((string CreditNr, NotificationResultCode Result, string ErrorMessage) result)
            {
                resultByCreditNr[result.CreditNr] = result;
            }

            var notificationSettings = notificationProcessSettingsFactory.GetByCreditType(creditType);
            int? fixedNotificationDueDay = envSettings.HasPerLoanDueDay ? new int?() : notificationSettings.NotificationDueDay;

            var successCount = 0;
            var failCount = 0;
            var deliveryFileCreated = false;
            List<string> errors = new List<string>();
            var w = Stopwatch.StartNew();

            var today = clock.Today;
            var customerPostalInfoRepository = new CustomerPostalInfoRepository(notificationSettings.AllowMissingCustomerAddress, customerClient, clientConfiguration);
            if (!skipNotify)
            {
                List<CreditNotificationGroup> creditGroupsToNotify;
                Dictionary<string, string> skippedCreditNrsWithReasons;
                Dictionary<string, CreditNotificationData> notificationDataByCreditNr;

                using (var context = creditContextFactory.CreateContext())
                {
                    creditGroupsToNotify = GetCreditGroupsToNotify(context, fixedNotificationDueDay, out skippedCreditNrsWithReasons, onlyTheseCreditNrs: onlyTheseCreditNrs);
                }

                var allCreditsNotified = creditGroupsToNotify
                    .SelectMany(x => x.CoNotifiedCredits.Concat(new[] { x.Common }))
                    .ToList();

                customerPostalInfoRepository.PreFetchCustomerPostalInfo(allCreditsNotified
                    .Where(x => x.Applicant1CustomerId.HasValue)
                    .Select(x => x.Applicant1CustomerId.Value)
                    .ToHashSetShared());
                                
                using (var context = creditContextFactory.CreateContext())
                {
                    var creditNrsToPrefetch = allCreditsNotified.Select(x => x.CreditNr).ToHashSetShared();
                    notificationDataByCreditNr = new Dictionary<string, CreditNotificationData>(creditNrsToPrefetch.Count);
                    var models = CreditDomainModel.PreFetchForCredits(context, creditNrsToPrefetch.ToArray(), envSettings);
                    var notifications = CreditNotificationDomainModel.CreateForSeveralCredits(new HashSet<string>(creditNrsToPrefetch), context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false);
                    Dictionary<string, string> mortgageLoanPropertyIdByCreditNr;
                    if (envSettings.IsMortgageLoansEnabled)
                        mortgageLoanPropertyIdByCreditNr = MortgageLoanCollateralService.GetPropertyIdByCreditNr(context, new HashSet<string>(creditNrsToPrefetch), false);
                    else
                        mortgageLoanPropertyIdByCreditNr = new Dictionary<string, string>();

                    var dueMonth = today.Month;
                    var dueYear = today.Year;
                    var paymentFreeMonthIdByCreditNr = context
                        .CreditHeadersQueryable
                        .Where(x => creditNrsToPrefetch.Contains(x.CreditNr))
                        .Select(x => new
                        {
                            x.CreditNr,
                            PaymentFreeMonthId = x
                            .CreditFuturePaymentFreeMonths
                            .Where(y => !y.CommitedByEventBusinessEventId.HasValue && !y.CancelledByBusinessEventId.HasValue && y.ForMonth.Year == dueYear && y.ForMonth.Month == dueMonth)
                            .OrderByDescending(y => y.Timestamp)
                            .Select(y => (int?)y.Id)
                            .FirstOrDefault()
                        })
                        .ToDictionary(x => x.CreditNr, x => x.PaymentFreeMonthId);

                    foreach (var creditNr in creditNrsToPrefetch)
                    {
                        notificationDataByCreditNr[creditNr] = new CreditNotificationData
                        {
                            Credit = models.Opt(creditNr),
                            Notifications = notifications.Opt(creditNr),
                            MortgageLoanPropertyId = mortgageLoanPropertyIdByCreditNr.Opt(creditNr),
                            PaymentFreeMonthId = paymentFreeMonthIdByCreditNr.OptSDefaultValue(creditNr)
                        };
                    }
                }
                var e = new NewCreditNotificationBusinessEventManager(currentUser, customerPostalInfoRepository,
                    notificationProcessSettingsFactory.GetByCreditType, envSettings, clock, clientConfiguration, paymentAccountService, creditContextFactory, 
                    x => loggingService.Warning(x), paymentOrderService);

                //Notify all co notification slaves
                var failedCoNotificationSlaveCreditNrs = new HashSet<string>();
                var okCoNotificationSlaveCreditNrs = new HashSet<string>();

                foreach (var creditGroup in creditGroupsToNotify)
                {
                    foreach (var coCredit in creditGroup.CoNotifiedCredits ?? new List<CreditNotificationStatusCommon>())
                    {
                        if (e.TryNotifySingleCredit(coCredit, creditGroup.CoNotificationId, null, notificationDataByCreditNr, renderer, out var errorMessage1,
                            observeResult: ObserveNotificationResult))
                        {
                            successCount++;
                            okCoNotificationSlaveCreditNrs.Add(coCredit.CreditNr);
                        }
                        else
                        {
                            failCount++;
                            skippedCreditNrsWithReasons[coCredit.CreditNr] = $"Error: {errorMessage1}";
                            errors.Add($"[credit={coCredit.CreditNr}]: {errorMessage1}");
                            failedCoNotificationSlaveCreditNrs.Add(coCredit.CreditNr);
                            ObserveNotificationResult((coCredit.CreditNr, NotificationResultCode.Error, errorMessage1));
                        }
                    }
                }

                LoadCoNotificationSlaveCredits(okCoNotificationSlaveCreditNrs, notificationDataByCreditNr);

                //Notify single credits and co notification masters
                foreach (var creditGroup in creditGroupsToNotify)
                {
                    var shouldCoNotify = creditGroup.CoNotificationId != null && creditGroup.CoNotifiedCredits.Count > 0;
                    if (shouldCoNotify && creditGroup.CoNotifiedCredits.Any(x => failedCoNotificationSlaveCreditNrs.Contains(x.CreditNr)))
                    {
                        //Skip co notification since one the slaves failed
                        shouldCoNotify = false;
                    }
                    if (e.TryNotifySingleCredit(creditGroup.Common,
                        shouldCoNotify ? creditGroup.CoNotificationId : null,
                        shouldCoNotify ? creditGroup.CoNotifiedCredits : new List<CreditNotificationStatusCommon>(),
                        notificationDataByCreditNr, renderer, out var errorMessage,
                        observeResult: ObserveNotificationResult))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        skippedCreditNrsWithReasons[creditGroup.Common.CreditNr] = $"Error: {errorMessage}";
                        errors.Add($"[credit={creditGroup.Common.CreditNr}]: {errorMessage}");
                        ObserveNotificationResult((creditGroup.Common.CreditNr, NotificationResultCode.Error, errorMessage));
                    }
                }
            }
            if (!skipDeliveryExport)
            {
                OutgoingCreditNotificationDeliveryFileHeader deliveryResult = null;
                deliveryResult = snailmailDeliveryService.DeliverLoans(errors, today, customerPostalInfoRepository, currentUser);
                if (deliveryResult != null)
                {
                    deliveryFileCreated = true;
                }
            }

            foreach (var error in errors)
            {
                loggingService.Warning($"CreditNotification: {error}");
            }

            w.Stop();

            loggingService.Information($"CreditNotification finished SuccessCount={successCount}, FailCount={failCount},TotalMilliseconds={w.ElapsedMilliseconds}");

            //Used by nScheduler
            var warnings = new List<string>();
            errors?.ForEach(x => warnings.Add(x));
            if (successCount == 0 && failCount == 0 && !deliveryFileCreated && !skipDeliveryExport && !envSettings.HasPerLoanDueDay)
            {
                warnings.Add("No notifications created or delivered");
            }

            return new CreateNotificationsResult { SuccessCount = successCount, FailCount = failCount, Errors = errors, TotalMilliseconds = w.ElapsedMilliseconds, Warnings = warnings, ResultByCreditNr = resultByCreditNr };
        }

        private void CancelDefaultedOrCompleteFullyPaidPaymentsPlans(List<string> onlyTheseCreditNrs)
        {
            if (!paymentPlanService.IsPaymentPlanEnabled)
                return;

            /*
             The reason we dont just check cancel here is that if the custome prepays by a large amount we dont want to complete the plan early so they
             have to start paying normal notification before the final due date. We then instead leave the plan open after the payment is placed
             and it will be completed here instead.
             */
            using(var context = creditContextFactory.CreateContext())
            {
                var b = new BusinessEventManagerOrServiceBase(context.CurrentUser, context.CoreClock, clientConfiguration);
                var cancelledEvent = new Lazy<BusinessEvent>(() => b.AddBusinessEvent(BusinessEventType.AlternatePaymentPlanCancelled, context));
                var completeEvent = new Lazy<BusinessEvent>(() => b.AddBusinessEvent(BusinessEventType.AlternatePaymentPlanCompleted, context));                
                paymentPlanService.CancelDefaultedOrCompleteFullyPaidPaymentsPlans(context, completeEvent, cancelledEvent, onlyTheseCreditNrs: onlyTheseCreditNrs);
                context.SaveChanges();
            }
            
        }

        /// <summary>
        /// So we notifiy child credits first and then main credits
        /// As soon as we reach the first main credit we want to ensure that we have up-to-date data about
        /// all child credits since their info is included on the main credits notification document.
        ///
        /// If the child credits notification header was created in this run it need to updated, otherwise it needs to be inserted.
        /// </summary>
        private void LoadCoNotificationSlaveCredits(ISet<string> creditNrs,
            Dictionary<string, CreditNotificationData> notificationDataByCreditNr)
        {
            if (!creditNrs.Any())
            {
                return;
            }

            using (var context = creditContextFactory.CreateContext())
            {
                var updatedModels = CreditDomainModel.PreFetchForCredits(context, creditNrs.ToArray(), envSettings);
                var updateNotifications = CreditNotificationDomainModel.CreateForSeveralCredits(new HashSet<string>(creditNrs), context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false);

                foreach (var m in updatedModels)
                {
                    var creditData = notificationDataByCreditNr[m.Key];
                    creditData.Credit = m.Value;
                }

                foreach (var n in updateNotifications)
                {
                    var creditData = notificationDataByCreditNr[n.Key];
                    creditData.Notifications = n.Value;
                }
            }
        }
    }

    public class CreateNotificationsResult
    {
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> Errors { get; set; }
        public long TotalMilliseconds { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, (string CreditNr, NotificationResultCode Result, string ErrorMessage)> ResultByCreditNr { get; set; }
    }

    public enum NotificationResultCode
    {
        NotificationCreated,
        Error,
        PaymentFreeMonth
    }

    public class CreditNotificationData
    {
        public CreditDomainModel Credit { get; set; }
        public Dictionary<int, CreditNotificationDomainModel> Notifications { get; set; }
        public string MortgageLoanPropertyId { get; set; }
        public int? PaymentFreeMonthId { get; set; }
    }
}