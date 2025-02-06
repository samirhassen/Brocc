using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.BusinessEvents.Utilities;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static nCredit.DbModel.BusinessEvents.NewCreditNotificationBusinessEventManager.CoNotificationPrintContext;
using static nCredit.DbModel.BusinessEvents.NewCreditNotificationBusinessEventManager.SingleCreditNotificationPrintContext;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewCreditNotificationBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        public NewCreditNotificationBusinessEventManager(
            INTechCurrentUserMetadata currentUser, ICustomerPostalInfoRepository customerPostalInfoRepository,
            Func<CreditType, NotificationProcessSettings> getNotificationSettings,
            ICreditEnvSettings envSettings,
            ICoreClock clock, IClientConfigurationCore clientConfiguration, PaymentAccountService paymentAccountService,
            CreditContextFactory creditContextFactory, Action<string> logWarning, PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.customerPostalInfoRepository = customerPostalInfoRepository;
            this.getNotificationSettings = getNotificationSettings;
            this.ocrNumberParser = new OcrNumberParser(clientConfiguration.Country.BaseCountry);
            this.envSettings = envSettings;
            this.paymentAccountService = paymentAccountService;
            this.creditContextFactory = creditContextFactory;
            this.logWarning = logWarning;
            this.paymentOrderService = paymentOrderService;
            incomingPaymentBankAccountNr = new Lazy<IBankAccountNumber>(() => paymentAccountService.GetIncomingPaymentBankAccountNr());
            uiPaymentOrder = new Lazy<List<PaymentOrderUiItem>>(() => paymentOrderService.GetPaymentOrderUiItems());
        }

        private readonly ICustomerPostalInfoRepository customerPostalInfoRepository;
        private Lazy<IBANToBICTranslator> bicFromIban = new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());
        private readonly Func<CreditType, NotificationProcessSettings> getNotificationSettings;
        private readonly OcrNumberParser ocrNumberParser;
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly Action<string> logWarning;
        private readonly PaymentOrderService paymentOrderService;
        private readonly Lazy<IBankAccountNumber> incomingPaymentBankAccountNr;
        private readonly Lazy<List<PaymentOrderUiItem>> uiPaymentOrder;


        public bool TryNotifySingleCredit(
            UngroupedNotificationService.CreditNotificationStatusCommon creditStatus,
            string coNotificationId,
            List<UngroupedNotificationService.CreditNotificationStatusCommon> coNotifiedCreditStatuses,
            Dictionary<string, CreditNotificationData> notificationDataByCreditNr,
            INotificationDocumentRenderer renderer,
            out string errorMessage,
            Action<(string CreditNr, NotificationResultCode Result, string ErrorMessage)> observeResult = null)
        {
            var today = Clock.Today;

            var creditNr = creditStatus.CreditNr;
            var model = notificationDataByCreditNr[creditNr].Credit;
            var oldNotificationsModel = notificationDataByCreditNr[creditNr].Notifications;
            var paymentFreeMonthId = notificationDataByCreditNr[creditNr].PaymentFreeMonthId;

            var s = new StringBuilder();
            string ocrPaymentReference;            
            bool makeMonthPaymentFree;
            bool willUseDirectDebit;

            var processSettings = getNotificationSettings(model.GetCreditType());
            var fixedDueDay = envSettings.HasPerLoanDueDay ? new int?() : processSettings.NotificationDueDay;

            var (shouldBeNotified, dueDatePre, skipReason) = UngroupedNotificationService.GetNotificationDueDateOrSkipReason(creditStatus, today, fixedDueDay);
            if (!shouldBeNotified)
            {
                errorMessage = skipReason;
                return false;
            }
            var dueDate = dueDatePre.Value;

            ocrPaymentReference = model.GetOcrPaymentReference(today);

            var amounts = GetAmounts(model, oldNotificationsModel, today, s, dueDate, uiPaymentOrder.Value);

            if (paymentFreeMonthId.HasValue)
            {
                makeMonthPaymentFree = amounts.TotalOverdueAmount <= 0m;
            }
            else
            {
                makeMonthPaymentFree = false;
            }

            willUseDirectDebit = envSettings.IsDirectDebitPaymentsEnabled && model.GetIsDirectDebitActive(today);

            using (var context = creditContextFactory.CreateContext())
            {
                if (!makeMonthPaymentFree)
                {
                    return HandleNotification(creditStatus, renderer, out errorMessage, s, amounts, ocrPaymentReference, dueDate, paymentFreeMonthId, context, willUseDirectDebit,
                        notificationDataByCreditNr,
                        today, coNotificationId, coNotifiedCreditStatuses, observeResult: observeResult);
                }
                else
                {
                    if (coNotificationId != null)
                        throw new Exception("Payment free months and co notification cannot be combined");
                    return HandlePaymentFreeMonth(creditNr, out errorMessage, amounts.InterestAmount, amounts.NotificationFeeAmount, dueDate, context,
                        paymentFreeMonthId, processSettings, observeResult: observeResult);
                }
            }
        }

        private bool HandleNotification(
            UngroupedNotificationService.CreditNotificationStatusCommon creditStatus, INotificationDocumentRenderer renderer,
            out string errorMessage, StringBuilder s, NotificationAmountsModel amounts, string ocrPaymentReference,
            DateTime dueDate, int? paymentFreeMonthId, ICreditContextExtended context, bool willUseDirectDebit,
            Dictionary<string, CreditNotificationData> notificationDataByCreditNr,
            DateTime today,
            string coNotificationId,
            List<UngroupedNotificationService.CreditNotificationStatusCommon> coNotifiedCreditStatuses,
            Action<(string CreditNr, NotificationResultCode Result, string ErrorMessage)> observeResult = null)
        {
            var creditNr = creditStatus.CreditNr;
            var credit = context.CreditHeadersQueryable.Single(x => x.CreditNr == creditNr);
            string commentText;
            BusinessEvent evt;

            var isCoNotificationSlave = coNotificationId != null && (coNotifiedCreditStatuses == null || coNotifiedCreditStatuses.Count == 0);

            
            var notification = CreateNotificationHeader(context, ocrPaymentReference, credit, dueDate, s.ToString(), amounts,
                coNotificationId, isCoNotificationSlave, creditStatus, out commentText, out evt);

            string notificationArchiveKey = null;
            if (!isCoNotificationSlave)
            {
                if (coNotifiedCreditStatuses == null || coNotifiedCreditStatuses.Count == 0)
                {
                    notificationArchiveKey = CreateSingleNotificationDocument(creditStatus, renderer, willUseDirectDebit, notification, amounts, notificationDataByCreditNr[creditNr]);
                }
                else
                {
                    notificationArchiveKey = CreateCoNotificationDocument(creditStatus, renderer, notification, amounts, notificationDataByCreditNr, today, coNotifiedCreditStatuses, dueDate, willUseDirectDebit);
                }
            }

            notification.PdfArchiveKey = notificationArchiveKey;
            if (paymentFreeMonthId.HasValue)
            {
                //This means the payment free month was disallowed due to having overdue, unpaid balance
                commentText += $" Pending paymentfree month ignored due to overdue, unpaid balance of {amounts.TotalOverdueAmount.ToString("f2", CommentFormattingCulture)}.";
                var f = context.CreditFuturePaymentFreeMonthsQueryable.Single(x => x.Id == paymentFreeMonthId.Value);
                f.CancelledByEvent = evt;
            }

            AddComment(
                commentText,
                BusinessEventType.NewNotification,
                context,
                credit: credit,
                attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(notificationArchiveKey));

            context.SaveChanges();

            observeResult?.Invoke((creditNr, NotificationResultCode.NotificationCreated, null));

            errorMessage = null;
            return true;
        }

        private NotificationAmountsModel GetAmounts(CreditDomainModel model,
            Dictionary<int, CreditNotificationDomainModel> oldNotificationsModel,
            DateTime today,
            StringBuilder s,
            DateTime dueDate,
            List<PaymentOrderUiItem> paymentOrder)
        {
            var m = new NotificationAmountsModel();

            var notNotifiedCapitalAmount = model.GetNotNotifiedCapitalBalance(today);
            var interestFromDate = model.GetNextInterestFromDate(today);
            s.Append($"Interest from {interestFromDate.ToString("d", CommentFormattingCulture)}. ");

            int nrOfInterestDays;
            m.InterestAmount = model.ComputeNotNotifiedInterestUntil(today, dueDate, out nrOfInterestDays);
            s.Append($"Interest days {nrOfInterestDays}. ");

            var amortizationModel = model.GetAmortizationModel(today);

            m.CapitalAmount = amortizationModel.GetNotificationCapitalAmount(today, dueDate, m.InterestAmount);
            if (m.CapitalAmount < 0m)
            {
                //Fall back on
                logWarning($"Notification: InterestAmount > Annuity on Credit {model.CreditNr}. CapitalAmount will be 0");
                s.Append($"InterestAmount > Annuity. Forcing CapitalAmount to 0");
                m.CapitalAmount = 0;
            }
            if (m.CapitalAmount > notNotifiedCapitalAmount)
            {
                s.Append("Capital > Not notified. Using all remaining capital instead. ");
                m.CapitalAmount = notNotifiedCapitalAmount;
            }
            else if (amortizationModel.ShouldCarryOverRemainingCapitalAmount(today, dueDate, notNotifiedCapitalAmount - m.CapitalAmount, PaymentPlanCalculation.DefaultSettings))
            {
                s.Append($"Default amortization {m.CapitalAmount.ToString("f2", CommentFormattingCulture)} would leave only {(notNotifiedCapitalAmount - m.CapitalAmount).ToString("f2", CommentFormattingCulture)} for the last notification. Using all remaining capital instead. ");
                m.CapitalAmount = notNotifiedCapitalAmount;
            }

            var promiseToPayDate = model.GetPromisedToPayDate(today);
            if (m.CapitalAmount > 0m && promiseToPayDate.HasValue && promiseToPayDate.Value > today && envSettings.IsPromiseToPayAmortizationFreedomEnabled)
            {
                s.Append($"Amortization {m.CapitalAmount.ToString("f2", CommentFormattingCulture)} removed  because of future promise to pay date {promiseToPayDate.Value.ToString("yyyy-MM-dd")}.");
                m.CapitalAmount = 0m;
            }

            m.NotificationFeeAmount = model.GetNotificationFee(today);
            m.OtherCosts = model.GetNotNotifiedNotificationCosts(today);
            m.OverdueOtherCosts = paymentOrder
                .Select(x => x.OrderItem)
                .Where(x => !x.IsBuiltin)
                .ToDictionary(
                    x => x.Code,
                    x => oldNotificationsModel.Values.Select(y => y.GetRemainingBalance(today, x)).Sum());

            //Compute overdue
            Func<CreditDomainModel.AmountType, decimal> getOverdueAmount = c =>
                oldNotificationsModel.Values.Select(x => x.GetRemainingBalance(today, c)).Sum();
            
            m.OverdueCapitalAmount = getOverdueAmount(CreditDomainModel.AmountType.Capital);
            m.OverdueInterestAmount = getOverdueAmount(CreditDomainModel.AmountType.Interest);
            m.OverdueNotificationFeeAmount = getOverdueAmount(CreditDomainModel.AmountType.NotificationFee);
            m.OverdueReminderFeeAmount = getOverdueAmount(CreditDomainModel.AmountType.ReminderFee);
            m.TotalOverdueAmount = m.OverdueCapitalAmount + m.OverdueInterestAmount + m.OverdueNotificationFeeAmount 
                + m.OverdueReminderFeeAmount + m.OverdueOtherCosts.Values.Sum();
            m.CurrentInterestRatePercent = model.GetInterestRatePercent(today);
            m.TotalUnpaidcreditCapitalAmount = model.GetBalance(CreditDomainModel.AmountType.Capital, today);

            return m;
        }

        public class NotificationAmountsModel
        {
            public decimal NotificationFeeAmount { get; internal set; }
            public decimal OverdueCapitalAmount { get; internal set; }
            public decimal OverdueInterestAmount { get; internal set; }
            public decimal OverdueNotificationFeeAmount { get; internal set; }
            public decimal OverdueReminderFeeAmount { get; internal set; }
            public decimal TotalOverdueAmount { get; internal set; }
            public decimal CurrentInterestRatePercent { get; internal set; }
            public decimal TotalUnpaidcreditCapitalAmount { get; internal set; }
            public decimal InterestAmount { get; internal set; }
            public decimal CapitalAmount { get; internal set; }
            public Dictionary<string, decimal> OtherCosts { get; set; }
            public Dictionary<string, decimal> OverdueOtherCosts { get; set; }

            public decimal GetAmount(PaymentOrderItem item, bool isOverdue)
            {
                if (item.IsBuiltin)
                {
                    switch(item.GetBuiltinAmountType())
                    {
                        case CreditDomainModel.AmountType.Capital: return isOverdue ? OverdueCapitalAmount : CapitalAmount;
                        case CreditDomainModel.AmountType.NotificationFee: return isOverdue ? OverdueNotificationFeeAmount : NotificationFeeAmount;
                        case CreditDomainModel.AmountType.Interest: return isOverdue ? OverdueInterestAmount : InterestAmount;
                        case CreditDomainModel.AmountType.ReminderFee: return isOverdue ? OverdueReminderFeeAmount : 0m;
                        default: throw new NotImplementedException();
                    }
                }
                else
                    return (isOverdue ? OverdueOtherCosts : OtherCosts)?.OptS(item.Code) ?? 0m;
            }

            public decimal GetTotalAmountCurrentNotification()
            {
                return CapitalAmount + InterestAmount + NotificationFeeAmount + (OtherCosts?.Values?.Sum() ?? 0m);
            }

            public List<NotificationAmountPrintContextModel> GetAmountsListPrintContext(List<PaymentOrderUiItem> paymentOrder, CultureInfo formattingCulture, bool isOverdue) =>
                paymentOrder.Select(x =>
                {
                    var amount = GetAmount(x.OrderItem, isOverdue);
                    return new NotificationAmountPrintContextModel
                    {
                        amount = amount == 0m ? null : amount.ToString("C", formattingCulture),
                        text = x.Text,
                        uniqueId = x.UniqueId,
                        isCustom = !x.OrderItem.IsBuiltin,
                        isBuiltinCapital = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital),
                        isBuiltinInterest = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Interest),
                        isBuiltinNotificationFee = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.NotificationFee),
                        isBuiltinReminderFee = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.ReminderFee)
                    };
                }).ToList();
        }

        private bool HandlePaymentFreeMonth(string creditNr, out string errorMessage, decimal interestAmount, decimal notificationFeeAmount, DateTime dueDate, ICreditContextExtended context,
            int? futurePaymentFreeMonthId, NotificationProcessSettings processSettings, Action<(string CreditNr, NotificationResultCode Result, string ErrorMessage)> observeResult = null)
        {
            var eventCode = BusinessEventType.PaymentFreeMonth;
            var evt = AddBusinessEvent(eventCode, context);

            var p = new CreditPaymentFreeMonth
            {
                CreatedByEvent = evt,
                CreditNr = creditNr,
                DueDate = dueDate,
                NotificationDate = Clock.Today,
                ChangedById = UserId,
                ChangedDate = Clock.Now,
                InformationMetaData = InformationMetadata
            };
            context.AddCreditPaymentFreeMonths(p);
            var trs = new List<AccountTransaction>();

            var newCapitalAmount = 0m;
            if (!processSettings.PaymentFreeMonthExcludeNotificationFee && notificationFeeAmount > 0m)
            {
                newCapitalAmount += notificationFeeAmount;
                trs.Add(CreateTransaction(TransactionAccountType.CapitalizedNotificationFee, notificationFeeAmount, evt.BookKeepingDate, evt, creditNr: creditNr, creditPaymentFreeMonth: p));
            }
            newCapitalAmount += interestAmount;
            trs.Add(CreateTransaction(TransactionAccountType.CapitalizedInterest, interestAmount, evt.BookKeepingDate, evt, creditNr: creditNr, creditPaymentFreeMonth: p));
            trs.Add(CreateTransaction(TransactionAccountType.CapitalDebt, newCapitalAmount, evt.BookKeepingDate, evt, creditNr: creditNr, creditPaymentFreeMonth: p));
            trs.Add(CreateTransaction(TransactionAccountType.NotNotifiedCapital, newCapitalAmount, evt.BookKeepingDate, evt, creditNr: creditNr, creditPaymentFreeMonth: p));

            AddDatedCreditString(DatedCreditStringCode.NextInterestFromDate.ToString(), dueDate.AddDays(1).ToString("yyyy-MM-dd"), creditNr, evt, context);

            AddComment($"New payment free month created instead of the notification with Due date: {p.DueDate.ToString("d", CommentFormattingCulture)}. Capitalized amount: {newCapitalAmount.ToString("f2", CommentFormattingCulture)}.",
                BusinessEventType.PaymentFreeMonth, context, creditNr: creditNr);

            context.AddAccountTransactions(trs.ToArray());

            if (futurePaymentFreeMonthId.HasValue)
            {
                var f = context.CreditFuturePaymentFreeMonthsQueryable.Single(x => x.Id == futurePaymentFreeMonthId.Value);
                f.CommitedByEvent = evt;
            }

            context.SaveChanges();

            observeResult?.Invoke((creditNr, NotificationResultCode.PaymentFreeMonth, null));

            errorMessage = null;
            return true;
        }

        private CreditNotificationHeader CreateNotificationHeader(ICreditContextExtended context, string ocrPaymentReference, CreditHeader credit, DateTime dueDate,
            string additionalCommentText, NotificationAmountsModel amounts,
            string coNotificationId, bool isCoNotificationSlave, UngroupedNotificationService.CreditNotificationStatusCommon creditStatus,
            out string commentText, out BusinessEvent evt)
        {
            var newNotificationEvent = new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.NewNotification.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            context.AddBusinessEvent(newNotificationEvent);
            evt = newNotificationEvent;
            
            var notification = new CreditNotificationHeader
            {
                ChangedById = UserId,
                BookKeepingDate = Now.ToLocalTime().Date,
                ChangedDate = Now,
                Credit = credit,
                DueDate = dueDate,
                InformationMetaData = InformationMetadata,
                NotificationDate = Now.ToLocalTime().Date,
                OcrPaymentReference = ocrPaymentReference,
                TransactionDate = Now.ToLocalTime().Date,
                IsCoNotificationMaster = coNotificationId == null ? new bool?() : (!isCoNotificationSlave),
                CoNotificationId = coNotificationId
            };
            context.AddCreditNotificationHeaders(notification);

            //These types of loans are typically notified on the same day they are created or the day after
            //When it's the day after NotificationDueDay will be one day off. It's better if these track for things like alternate payment plans even though there is only one notification sent.
            if (creditStatus.SinglePaymentLoanRepaymentDays.HasValue && creditStatus.PerLoanDueDay.HasValue && creditStatus.PerLoanDueDay.Value != dueDate.Day)
                AddDatedCreditValue(DatedCreditValueCode.NotificationDueDay, dueDate.Day, newNotificationEvent, context, credit: credit);

            var totalAmount = 0m;
            if (amounts.CapitalAmount > 0m)
            {
                totalAmount += amounts.CapitalAmount;
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.NotNotifiedCapital,
                    -amounts.CapitalAmount,
                    newNotificationEvent.BookKeepingDate,
                    newNotificationEvent,
                    credit: credit,
                    notification: notification));
            }
            else if (amounts.CapitalAmount < 0m)
                throw new Exception($"{credit.CreditNr}: capitalAmount cannot be negative");

            if (amounts.InterestAmount > 0m)
            {
                totalAmount += amounts.InterestAmount;
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.InterestDebt,
                    amounts.InterestAmount,
                    newNotificationEvent.BookKeepingDate,
                    newNotificationEvent,
                    credit: credit,
                    notification: notification));
            }
            else if (amounts.InterestAmount < 0m)
                throw new Exception("capitalAmount cannot be negative");

            if (amounts.NotificationFeeAmount > 0m && !isCoNotificationSlave)
            {
                totalAmount += amounts.NotificationFeeAmount;
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.NotificationFeeDebt,
                    amounts.NotificationFeeAmount,
                    newNotificationEvent.BookKeepingDate,
                    newNotificationEvent,
                    credit: credit,
                    notification: notification));
            }
            else if (amounts.NotificationFeeAmount < 0m)
                throw new Exception("capitalAmount cannot be negative");

            if(amounts.OtherCosts != null && amounts.OtherCosts.Count > 0)
            {
                foreach(var otherCost in amounts.OtherCosts)
                {
                    totalAmount += otherCost.Value;
                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotNotifiedNotificationCost,
                        -otherCost.Value,
                        newNotificationEvent.BookKeepingDate,
                        newNotificationEvent,
                        credit: credit,
                        subAccountCode: otherCost.Key));
                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotificationCost,
                        otherCost.Value,
                        newNotificationEvent.BookKeepingDate,
                        newNotificationEvent,
                        credit: credit,
                        notification: notification,
                        subAccountCode: otherCost.Key));                    
                }
            }

            AddDatedCreditString(DatedCreditStringCode.NextInterestFromDate.ToString(), dueDate.AddDays(1).ToString("yyyy-MM-dd"), credit, newNotificationEvent, context);

            if (totalAmount <= 0m)
            {
                UpdateNotificationOnFullyPaid(notification, Clock, UserId);
                commentText = $"New zero amount notification created and closed. Due date would have been: {notification.DueDate.ToString("d", CommentFormattingCulture)}. {additionalCommentText}";
            }
            else
            {
                commentText = $"New notification created. Due date: {notification.DueDate.ToString("d", CommentFormattingCulture)}. Total amount: {totalAmount.ToString("f2", CommentFormattingCulture)}. {additionalCommentText}";
            }

            return notification;
        }

        public static void UpdateNotificationOnFullyPaid(CreditNotificationHeader creditNotificationHeader, ICoreClock clock, int userId)
        {
            creditNotificationHeader.ClosedTransactionDate = clock.Today;
            creditNotificationHeader.ChangedById = userId;
            creditNotificationHeader.ChangedDate = clock.Now;
        }

        private string CreateSingleNotificationDocument(
            UngroupedNotificationService.CreditNotificationStatusCommon creditStatus, INotificationDocumentRenderer renderer,
            bool willUseDirectDebit, CreditNotificationHeader notification, NotificationAmountsModel amounts, CreditNotificationData notificationData)
        {
            var totalAmountCurrentNotification = amounts.GetTotalAmountCurrentNotification();

            var totalAmountIncludingOtherNotifications = (totalAmountCurrentNotification + amounts.TotalOverdueAmount);

            var customerPortalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(creditStatus.Applicant1CustomerId.Value);

            var creditModel = notificationData.Credit;

            //Create the pdf
            var printContext = new SingleCreditNotificationPrintContext
            {
                creditNr = creditStatus.CreditNr,
                areaAndZipcode = $"{customerPortalInfo.ZipCode} {customerPortalInfo.PostArea}",
                streetAddress = customerPortalInfo.StreetAddress,
                fullName = customerPortalInfo.GetPersonPropertyOrNull(x => x.FullName),
                companyName = customerPortalInfo.GetCompanyPropertyOrNull(x => x.CompanyName),
                dueDate = notification.DueDate.ToString("d", PrintFormattingCulture),
                dueMonth = FormatMonthCultureAware(notification.DueDate),
                notificationDate = notification.NotificationDate.ToString("d", PrintFormattingCulture),
                ocrPaymentReference = ocrNumberParser.Parse(notification.OcrPaymentReference).DisplayForm,
                totalAmountCurrentNotification = totalAmountCurrentNotification.ToString("C", PrintFormattingCulture),
                mortgageLoanPropertyId = notificationData.MortgageLoanPropertyId,
                currentNotificationAmounts = new SingleCreditNotificationPrintContext.Currentnotificationamounts 
                {
                    capitalAmount = amounts.CapitalAmount == 0m ? null : amounts.CapitalAmount.ToString("C", PrintFormattingCulture),
                    interestAmount = amounts.InterestAmount == 0m ? null : amounts.InterestAmount.ToString("C", PrintFormattingCulture),
                    notificationfeeAmount = amounts.NotificationFeeAmount == 0m ? null : amounts.NotificationFeeAmount.ToString("C", PrintFormattingCulture)
                },
                currentNotificationAmountsList = amounts.GetAmountsListPrintContext(uiPaymentOrder.Value, PrintFormattingCulture, isOverdue: false),
                overdue = amounts.TotalOverdueAmount == 0m ? null : new SingleCreditNotificationPrintContext.Overdue
                {
                    totalOverDueAmount = amounts.TotalOverdueAmount.ToString("C", PrintFormattingCulture),
                    overdueNotificationAmounts = new SingleCreditNotificationPrintContext.Overduenotificationamounts
                    {
                        capitalAmount = amounts.OverdueCapitalAmount == 0m ? null : amounts.OverdueCapitalAmount.ToString("C", PrintFormattingCulture),
                        interestAmount = amounts.OverdueInterestAmount == 0m ? null : amounts.OverdueInterestAmount.ToString("C", PrintFormattingCulture),
                        notificationfeeAmount = amounts.OverdueNotificationFeeAmount == 0m ? null : amounts.OverdueNotificationFeeAmount.ToString("C", PrintFormattingCulture),
                        reminderFeeAmount = amounts.OverdueReminderFeeAmount == 0m ? null : amounts.OverdueReminderFeeAmount.ToString("C", PrintFormattingCulture)
                    },
                    overdueNotificationAmountsList = amounts.GetAmountsListPrintContext(uiPaymentOrder.Value, PrintFormattingCulture, isOverdue: true),
                },
                totalAmountIncludingOtherNotifications = totalAmountIncludingOtherNotifications.ToString("C", PrintFormattingCulture),
                totalAmountIncludingOtherNotificationsRaw = totalAmountIncludingOtherNotifications.ToString("F2", PrintFormattingCulture),
                currentInterestRatePercent = amounts.CurrentInterestRatePercent.ToString("F2", PrintFormattingCulture),
                currentInterestRatePercentMonthly = Math.Round(amounts.CurrentInterestRatePercent / 12m, 2).ToString("F2", PrintFormattingCulture),
                totalUnpaidcreditCapitalAmount = amounts.TotalUnpaidcreditCapitalAmount.ToString("N2", PrintFormattingCulture),
                willUseDirectDebit = willUseDirectDebit,
                currentInterestFromDate = notificationData.Credit.GetNextInterestFromDate(Clock.Today).ToString("d", PrintFormattingCulture),
                currentInterestToDate = notification.DueDate.ToString("d", PrintFormattingCulture)
            };

            if (ClientCfg.Country.BaseCountry == "FI")
            {
                var paymentIban = (IBANFi)incomingPaymentBankAccountNr.Value;

                printContext.paymentIban = paymentAccountService.FormatIncomingBankAccountNrForDisplay(paymentIban);
                printContext.paymentBic = bicFromIban.Value.InferBic(paymentIban);
                printContext.paymentBankName = bicFromIban.Value.InferBankName(paymentIban);
            }
            else if (ClientCfg.Country.BaseCountry == "SE")
            {
                printContext.paymentBankGiroNr = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr.Value);
            }
            else
                throw new NotImplementedException();

            var printContextDict = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(printContext));
            return renderer.RenderDocumentToArchive(creditModel.GetCreditType(), false, printContextDict,
                $"creditnotification_{creditStatus.CreditNr}_{notification.DueDate.ToString("yyyy-MM-dd")}.pdf");
        }

        private string CreateCoNotificationDocument(
                   UngroupedNotificationService.CreditNotificationStatusCommon creditStatus,
                   INotificationDocumentRenderer renderer,
                   CreditNotificationHeader notification,
                   NotificationAmountsModel creditAmounts,
                   Dictionary<string, CreditNotificationData> notificationDataByCreditNr,
                   DateTime today,
                   List<UngroupedNotificationService.CreditNotificationStatusCommon> coNotifiedCreditStatuses,
                   DateTime dueDate,
                   bool willUseDirectDebit)
        {
            var customerId = creditStatus.Applicant1CustomerId.Value;

            var customerPortalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(customerId);
            var mainCredit = notificationDataByCreditNr[creditStatus.CreditNr].Credit;

            var notifiedAmountsByCreditNr = new Dictionary<string, NotificationAmountsModel>();
            notifiedAmountsByCreditNr[creditStatus.CreditNr] = creditAmounts;
            foreach (var coAmount in NewCreditCoNotificationHelper.GetAmounts(coNotifiedCreditStatuses, 
                x => notificationDataByCreditNr[x].Credit,
                x => notificationDataByCreditNr[x].Notifications, today, dueDate, paymentOrderService.GetPaymentOrderUiItems()))
            {
                notifiedAmountsByCreditNr[coAmount.Key] = coAmount.Value;
            }

            var allCredits = new List<UngroupedNotificationService.CreditNotificationStatusCommon>();
            allCredits.Add(creditStatus);
            allCredits.AddRange(coNotifiedCreditStatuses);

            var sharedTotalOverDueAmount = 0m;
            var sharedCurrentNotificationsAmount = 0m;
            var printContext = new CoNotificationPrintContext
            {
                areaAndZipcode = $"{customerPortalInfo.ZipCode} {customerPortalInfo.PostArea}",
                streetAddress = customerPortalInfo.StreetAddress,
                fullName = customerPortalInfo.GetPersonPropertyOrNull(x => x.FullName),
                companyName = customerPortalInfo.GetCompanyPropertyOrNull(x => x.CompanyName),
                dueDate = notification.DueDate.ToString("d", PrintFormattingCulture),
                dueMonth = FormatMonthCultureAware(notification.DueDate),
                notificationDate = notification.NotificationDate.ToString("d", PrintFormattingCulture),
                sharedOcrPaymentReference = ocrNumberParser.Parse(mainCredit.GetDatedCreditString(today, DatedCreditStringCode.SharedOcrPaymentReference, null)).DisplayForm,
                willUseDirectDebit = willUseDirectDebit,
                mortgageLoanAgreementNr = mainCredit.GetDatedCreditString(today, DatedCreditStringCode.MortgageLoanAgreementNr, null, allowMissing: true),
                totalUnpaidcreditCapitalAmount = allCredits.Sum(x => notifiedAmountsByCreditNr[x.CreditNr].TotalUnpaidcreditCapitalAmount).ToString("N2", PrintFormattingCulture),
                mortgageLoanPropertyId = notificationDataByCreditNr[creditStatus.CreditNr].MortgageLoanPropertyId,
                allCredits = allCredits.Select((x, i) =>
                {
                    var creditModel = notificationDataByCreditNr[x.CreditNr].Credit;
                    var amounts = notifiedAmountsByCreditNr[x.CreditNr];
                    var totalAmountCurrentNotification = amounts.GetTotalAmountCurrentNotification();
                    var totalAmountIncludingOtherNotifications = (totalAmountCurrentNotification + amounts.TotalOverdueAmount);
                    sharedTotalOverDueAmount += amounts.TotalOverdueAmount;
                    sharedCurrentNotificationsAmount += amounts.GetTotalAmountCurrentNotification();
                    return new CoNotificationPrintContext.Credit
                    {
                        creditNr = x.CreditNr,
                        isFirst = (i == 0) ? "true" : null,
                        ocrPaymentReference = creditModel.GetOcrPaymentReference(today),
                        totalUnpaidcreditCapitalAmount = amounts.TotalUnpaidcreditCapitalAmount.ToString("N2", PrintFormattingCulture),
                        currentNotificationAmounts = new CoNotificationPrintContext.NotificationsAmount
                        {
                            capitalAmount = FormatMoney(amounts.CapitalAmount),
                            interestAmount = FormatMoney(amounts.InterestAmount),
                            notificationfeeAmount = FormatMoney(amounts.NotificationFeeAmount),
                            reminderFeeAmount = FormatMoney(amounts.OverdueReminderFeeAmount),
                        },
                        currentNotificationAmountsList = amounts.GetAmountsListPrintContext(uiPaymentOrder.Value, PrintFormattingCulture, isOverdue: false),
                        overdue = amounts.TotalOverdueAmount > 0m ? new CoNotificationPrintContext.OverDueModel
                        {
                            totalOverDueAmount = FormatMoney(amounts.TotalOverdueAmount),
                            overdueNotificationAmounts = new SingleCreditNotificationPrintContext.Overduenotificationamounts
                            {
                                capitalAmount = amounts.OverdueCapitalAmount == 0m ? null : amounts.OverdueCapitalAmount.ToString("C", PrintFormattingCulture),
                                interestAmount = amounts.OverdueInterestAmount == 0m ? null : amounts.OverdueInterestAmount.ToString("C", PrintFormattingCulture),
                                notificationfeeAmount = amounts.OverdueNotificationFeeAmount == 0m ? null : amounts.OverdueNotificationFeeAmount.ToString("C", PrintFormattingCulture),
                                reminderFeeAmount = amounts.OverdueReminderFeeAmount == 0m ? null : amounts.OverdueReminderFeeAmount.ToString("C", PrintFormattingCulture)
                            },
                            overdueNotificationAmountsList = amounts.GetAmountsListPrintContext(uiPaymentOrder.Value, PrintFormattingCulture, isOverdue: true),
                        } : null,
                        totalAmountIncludingOtherNotifications = FormatMoney(totalAmountIncludingOtherNotifications),
                        totalAmountIncludingOtherNotificationsRaw = totalAmountIncludingOtherNotifications.ToString("F2", PrintFormattingCulture),
                        totalAmountCurrentNotification = totalAmountCurrentNotification.ToString("C", PrintFormattingCulture),
                        currentInterestRatePercent = amounts.CurrentInterestRatePercent.ToString("F2", PrintFormattingCulture),
                        currentInterestRatePercentMonthly = Math.Round(amounts.CurrentInterestRatePercent / 12m, 2).ToString("F2", PrintFormattingCulture),
                        currentInterestFromDate = creditModel.GetNextInterestFromDateWithValueLessThan(Clock.Today, notification.DueDate).ToString("d", PrintFormattingCulture),
                        currentInterestToDate = notification.DueDate.ToString("d", PrintFormattingCulture)
                    };
                }).ToList(),

                //Set separately
                paymentBankGiroNr = null,
                paymentBankName = null,
                paymentBic = null,
                paymentIban = null,
                sharedTotalOverDueAmount = null,
                sharedTotalAmountIncludingOtherNotifications = null
            };

            printContext.sharedTotalOverDueAmount = FormatMoney(sharedTotalOverDueAmount);
            printContext.sharedTotalAmountIncludingOtherNotifications = FormatMoney(sharedTotalOverDueAmount + sharedCurrentNotificationsAmount);

            if (ClientCfg.Country.BaseCountry == "FI")
            {
                var paymentIban = (IBANFi)incomingPaymentBankAccountNr.Value;
                printContext.paymentIban = paymentAccountService.FormatIncomingBankAccountNrForDisplay(paymentIban);
                printContext.paymentBic = bicFromIban.Value.InferBic(paymentIban);
                printContext.paymentBankName = bicFromIban.Value.InferBankName(paymentIban);
            }
            else if (ClientCfg.Country.BaseCountry == "SE")
            {
                printContext.paymentBankGiroNr = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr.Value);
            }
            else
                throw new NotImplementedException();

            var contextDict = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(printContext));
            return renderer.RenderDocumentToArchive(mainCredit.GetCreditType(), true, contextDict,
                $"creditnotification_{creditStatus.CreditNr}_{notification.DueDate.ToString("yyyy-MM-dd")}.pdf");
        }

        private string FormatMoney(decimal? d)
        {
            return d.HasValue ? (d.Value == 0m ? null : d.Value.ToString("C", PrintFormattingCulture)) : null;
        }

        #region "SingleCreditNotificationPrintContext"

        public class SingleCreditNotificationPrintContext
        {
            public string mortgageLoanPropertyId { get; set; }

            public string notificationDate { get; set; }
            public string dueMonth { get; set; }
            public string dueDate { get; set; }
            public string creditNr { get; set; }
            public string fullName { get; set; }
            public string companyName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string paymentIban { get; set; }
            public string paymentBic { get; set; }
            public string paymentBankName { get; set; }
            public string ocrPaymentReference { get; set; }
            public string totalAmountIncludingOtherNotifications { get; set; }
            public string totalAmountIncludingOtherNotificationsRaw { get; set; }
            public string currentInterestRatePercent { get; set; }
            public string currentInterestRatePercentMonthly { get; set; }
            public string totalUnpaidcreditCapitalAmount { get; set; }
            public Currentnotificationamounts currentNotificationAmounts { get; set; } //TODO: Drop this
            public List<NotificationAmountPrintContextModel> currentNotificationAmountsList { get; set; }
            public string totalAmountCurrentNotification { get; set; }
            public Overdue overdue { get; set; }
            public string paymentBankGiroNr { get; set; }
            public bool willUseDirectDebit { get; set; }
            public string currentInterestFromDate { get; set; }
            public string currentInterestToDate { get; set; }

            public class Currentnotificationamounts
            {
                public string interestDifferenceCostAmount { get; set; }
                public string notificationfeeAmount { get; set; }
                public string capitalAmount { get; set; }
                public string interestAmount { get; set; }
            }

            public class Overdue
            {
                public string totalOverDueAmount { get; set; }
                public Overduenotificationamounts overdueNotificationAmounts { get; set; } //TODO: Drop this
                public List<NotificationAmountPrintContextModel> overdueNotificationAmountsList { get; set; }
            }

            public class Overduenotificationamounts
            {
                public string reminderFeeAmount { get; set; }
                public string notificationfeeAmount { get; set; }
                public string capitalAmount { get; set; }
                public string interestAmount { get; set; }
            }
        }

        #endregion "SingleCreditNotificationPrintContext"

        #region "CoNotificationPrintContext"

        public class CoNotificationPrintContext
        {
            public string mortgageLoanPropertyId { get; set; }

            public string totalUnpaidcreditCapitalAmount { get; set; }

            public bool willUseDirectDebit { get; set; }
            public string notificationDate { get; set; }
            public string dueMonth { get; set; }
            public string dueDate { get; set; }
            public string fullName { get; set; }
            public string companyName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string paymentIban { get; set; }
            public string paymentBic { get; set; }
            public string paymentBankName { get; set; }
            public string paymentBankGiroNr { get; set; }
            public string sharedOcrPaymentReference { get; set; }
            public string sharedTotalAmountIncludingOtherNotifications { get; set; }
            public string sharedTotalOverDueAmount { get; set; }
            public string mortgageLoanAgreementNr { get; set; }

            public List<Credit> allCredits { get; set; }

            public class Credit
            {
                public string isFirst { get; set; }
                public string creditNr { get; set; }
                public string ocrPaymentReference { get; set; }
                public string totalUnpaidcreditCapitalAmount { get; set; }
                public NotificationsAmount currentNotificationAmounts { get; set; } //TODO: Drop
                public List<NotificationAmountPrintContextModel> currentNotificationAmountsList { get; set; }
                public OverDueModel overdue { get; set; }
                public string totalAmountIncludingOtherNotifications { get; set; }
                public string totalAmountIncludingOtherNotificationsRaw { get; set; }
                public string totalAmountCurrentNotification { get; set; }
                public string currentInterestRatePercent { get; set; }
                public string currentInterestRatePercentMonthly { get; set; }
                public string currentInterestFromDate { get; set; }
                public string currentInterestToDate { get; set; }
            }

            public class NotificationsAmount
            {
                public string capitalAmount { get; set; }
                public string interestAmount { get; set; }
                public string notificationfeeAmount { get; set; }
                public string reminderFeeAmount { get; set; }                
            }

            public class OverDueModel
            {
                public string totalOverDueAmount { get; set; }
                public SingleCreditNotificationPrintContext.Overduenotificationamounts overdueNotificationAmounts { get; set; } //TODO: Drop
                public List<NotificationAmountPrintContextModel> overdueNotificationAmountsList { get; set; }
            }
        }

        #endregion "CoNotificationPrintContext"
    }
}