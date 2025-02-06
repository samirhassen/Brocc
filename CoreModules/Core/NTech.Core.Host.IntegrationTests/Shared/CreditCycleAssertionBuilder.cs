using Dapper;
using nCredit;
using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.Shared
{
    using AssertionType = Action<(ICreditContextExtended Context, CreditSupportShared Support, int MonthNr, int DayNr, string Prefix)>;

    internal class CreditCycleAssertionBuilder
    {
        private CreditCycleAssertion a = new CreditCycleAssertion();
        internal int? forMonthNr = 1; //nullable so we can remove it in End(). This is to prevent using this instead of t.MonthNr from inside the assertions as fromMonthNr will always be the last month

        internal static CreditCycleAssertionBuilder Begin() => new CreditCycleAssertionBuilder();

        internal CreditCycleAssertionBuilder ExpectToggleNextMonthPaymentFreeAllowed(int dayNr, string creditNr) => ExpectToggleNextMonthPaymentFree(dayNr, creditNr, true);
        internal CreditCycleAssertionBuilder ExpectToggleNextMonthPaymentFreeDisabled(int dayNr, string creditNr) => ExpectToggleNextMonthPaymentFree(dayNr, creditNr, false);
        internal CreditCycleAssertionBuilder ExpectToggleNextMonthPaymentFreeNotShown(int dayNr, string creditNr) => ExpectToggleNextMonthPaymentFree(dayNr, creditNr, null);

        private CreditCycleAssertionBuilder ExpectToggleNextMonthPaymentFree(int dayNr, string creditNr, bool? isAllowed, int? paymentFreeMonthNr = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var s = t.Support.GetRequiredService<AmortizationPlanService>();
                var amortizationPlan = s.GetAmortizationPlan(creditNr);

                AmortizationPlanUiModel.AmortizationPlanUiItem? monthItem;
                if (paymentFreeMonthNr.HasValue)
                {
                    var paymentFreeMonth = GetMonth(t, paymentFreeMonthNr.Value);
                    monthItem = amortizationPlan.Items.Where(x => x.IsFutureItem && x.FutureItemDueDate.HasValue && Month.ContainingDate(x.FutureItemDueDate.Value).Equals(paymentFreeMonth)).FirstOrDefault();
                }
                else
                    monthItem = amortizationPlan.Items.Where(x => x.IsFutureItem).FirstOrDefault();

                var toggleLabel = isAllowed.HasValue ? (isAllowed.Value ? "allowed" : "disabled") : "not shown";
                var monthNrLabel = paymentFreeMonthNr.HasValue ? $"month {paymentFreeMonthNr.Value}" : "next month"; ;
                Assert.That(monthItem?.IsPaymentFreeMonthAllowed, Is.EqualTo(isAllowed), $"{t.Prefix}Expect toggle payment free month on {creditNr} to be {toggleLabel} for {monthNrLabel}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectPaymentPlaced(int dayNr, string creditNr, decimal? notifiedCapitalAmount = null, decimal? notifiedInterestAmount = null,
            decimal? notNotifiedCapitalAmount = null, decimal? notifiedReminderFeeAmount = null, (string CustomCode, decimal Amount)? notifiedCustomAmount = null,
            decimal? initialPaymentAmount = null, decimal? leftUnplacedAmount = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Context.CoreClock.Today;
                var paymentPlacementTransactions = t
                    .Context
                    .TransactionsQueryable
                    .Where(x => x.TransactionDate == today && x.IncomingPaymentId.HasValue && x.CreditNr == creditNr)
                    .Select(x => new { x.IncomingPaymentId, x.Amount, x.AccountCode, x.SubAccountCode, x.CreditNotificationId })
                    .ToList();
                var paymentId = paymentPlacementTransactions.Select(x => x.IncomingPaymentId).FirstOrDefault();
                decimal GetActualAmount(string code, bool isNotNotified = false) => -paymentPlacementTransactions.Where(x => x.AccountCode == code && x.CreditNotificationId.HasValue != isNotNotified).Sum(x => x.Amount);
                Assert.Multiple(() =>
                {
                    if (notifiedCapitalAmount.HasValue)
                        Assert.That(GetActualAmount(TransactionAccountType.CapitalDebt.ToString()), Is.EqualTo(notifiedCapitalAmount.Value), $"{t.Prefix}Placed CapitalDebt against {creditNr}");

                    if (notifiedInterestAmount.HasValue)
                        Assert.That(GetActualAmount(TransactionAccountType.InterestDebt.ToString()), Is.EqualTo(notifiedInterestAmount.Value), $"{t.Prefix}Placed InterestDebt against {creditNr}");

                    if (notNotifiedCapitalAmount.HasValue)
                        Assert.That(GetActualAmount(TransactionAccountType.CapitalDebt.ToString(), isNotNotified: true), Is.EqualTo(notNotifiedCapitalAmount.Value), $"{t.Prefix}Placed Extra amortization against {creditNr}");

                    if (notifiedReminderFeeAmount.HasValue)
                        Assert.That(GetActualAmount(TransactionAccountType.ReminderFeeDebt.ToString()), Is.EqualTo(notifiedReminderFeeAmount.Value), $"{t.Prefix}Placed ReminderFeeDebt against {creditNr}");

                    if (notifiedCustomAmount.HasValue)
                    {
                        var actualAmount = -paymentPlacementTransactions.Where(x => x.AccountCode == TransactionAccountType.NotificationCost.ToString()
                            && x.CreditNotificationId.HasValue && x.SubAccountCode == notifiedCustomAmount.Value.CustomCode).Sum(x => x.Amount);
                        Assert.That(actualAmount, Is.EqualTo(notifiedCustomAmount.Value.Amount), $"{t.Prefix}Placed {notifiedCustomAmount.Value.CustomCode} against {creditNr}");
                    }

                });

                if (!initialPaymentAmount.HasValue && !leftUnplacedAmount.HasValue)
                    return;
                Assert.That(paymentId.HasValue, Is.True, $"{t.Prefix}Missing payment placed against {creditNr}");
                var payment = t.Context.IncomingPaymentHeadersQueryable.Where(x => x.Id == paymentId.Value).Select(x => new
                {
                    InitialAmount = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString() && y.Amount > 0).Sum(y => (decimal?)y.Amount) ?? 0m,
                    LeftUnplacedAmount = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.UnplacedPayment.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                }).Single();
                Assert.Multiple(() =>
                {
                    if (initialPaymentAmount.HasValue)
                        Assert.That(payment.InitialAmount, Is.EqualTo(initialPaymentAmount.Value), $"{t.Prefix}Initial payment amount for payment placed against {creditNr}");
                    if (leftUnplacedAmount.HasValue)
                        Assert.That(payment.LeftUnplacedAmount, Is.EqualTo(leftUnplacedAmount.Value), $"{t.Prefix}Left unplaced amount for payment placed against {creditNr}");
                });

            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNotification(int dayNr, string creditNr, int? dueDay, decimal? initialAmount = null, bool? isSnailMailDeliveryExpected = null,
            decimal? interestAmount = null, decimal? capitalAmount = null, decimal? notificationFeeAmount = null, (string Code, decimal Amount)? notificationCost = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                if (creditNr == null) return;

                var paymentOrder = t.Support.GetRequiredService<PaymentOrderService>().GetPaymentOrderItems();
                var notifications = CreditNotificationDomainModel.CreateForCredit(creditNr, t.Context, paymentOrder, true);
                var notificationDate = t.Support.CurrentMonth.GetDayDate(dayNr);
                var notification = notifications.Values.SingleOrDefault(x => x.NotificationDate == notificationDate);
                Assert.That(notification, Is.Not.Null, $"{t.Prefix}Expected notification on {creditNr}");

                if (dueDay.HasValue)
                {
                    var currentMonth = t.Support.CurrentMonth;
                    var expectedMonth = dueDay.Value < t.Support.Clock.Today.Day ? currentMonth.NextMonth : currentMonth;

                    Assert.That(notification?.DueDate, Is.EqualTo(expectedMonth.GetDayDate(dueDay.Value)), $"{t.Prefix}Notification on {creditNr} due date");
                }

                if (initialAmount.HasValue)
                    Assert.That(notification?.GetInitialAmount(t.Support.Clock.Today), Is.EqualTo(initialAmount.Value), $"{t.Prefix}Notification on {creditNr} notified amount");

                if (interestAmount.HasValue)
                    Assert.That(notification?.GetInitialAmount(t.Support.Clock.Today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Interest)),
                        Is.EqualTo(interestAmount.Value), $"{t.Prefix}Notification on {creditNr} interest amount");

                if (capitalAmount.HasValue)
                    Assert.That(notification?.GetInitialAmount(t.Support.Clock.Today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital)),
                        Is.EqualTo(capitalAmount.Value), $"{t.Prefix}Notification on {creditNr} capital amount");

                if (notificationFeeAmount.HasValue)
                    Assert.That(notification?.GetInitialAmount(t.Support.Clock.Today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.NotificationFee)),
                        Is.EqualTo(notificationFeeAmount.Value), $"{t.Prefix}Notification on {creditNr} notification fee amount");

                if (notificationCost.HasValue)
                    Assert.That(notification?.GetInitialAmount(t.Support.Clock.Today, PaymentOrderItem.FromCustomCostCode(notificationCost.Value.Code)),
                        Is.EqualTo(notificationCost.Value.Amount), $"{t.Prefix}Notification on {creditNr} {notificationCost.Value.Code} amount");

                if (isSnailMailDeliveryExpected.HasValue)
                {
                    var today = t.Support.Clock.Today;
                    var hasSnailMailDelivery = t.Context.CreditNotificationHeadersQueryable.Any(x => x.CreditNr == creditNr && x.DueDate == notification!.DueDate && x.DeliveryFile.TransactionDate == today);
                    Assert.That(hasSnailMailDelivery, Is.EqualTo(isSnailMailDeliveryExpected.Value), $"{t.Prefix}Notification on {creditNr} snail mail delivery {(isSnailMailDeliveryExpected == true ? "expected" : "not expected")}");
                }
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNoNotificationCreated(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                if (creditNr == null) return;
                var notificationDate = t.Support.CurrentMonth.GetDayDate(dayNr);

                var hasNotification = t.Context.CreditNotificationHeadersQueryable.Any(x => x.CreditNr == creditNr && x.NotificationDate == notificationDate);
                Assert.That(hasNotification, Is.False, $"{t.Prefix}No notification on {creditNr} expected");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNotificationFullyPaid(int dayNr, string creditNr, int dueDay, int? fromMonthNr = null)
        {
            string monthLabel = (fromMonthNr.HasValue ? fromMonthNr.Value : forMonthNr!.Value).ToString(); //do not move this into AddAssertion
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var dueDate = Month.ContainingDate(!fromMonthNr.HasValue ? today : today.AddMonths(-(t.MonthNr - fromMonthNr.Value))).GetDayDate(dueDay);
                var isFullyPaid = t.Context
                    .CreditNotificationHeadersQueryable
                    .Any(x => x.CreditNr == creditNr && x.DueDate == dueDate && x.ClosedTransactionDate == today && !x.Transactions.Any(y => y.WriteoffId.HasValue));
                Assert.That(isFullyPaid, Is.True, $"{t.Prefix}Notification on {creditNr} due {dueDay} month {monthLabel} should be fully paid");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectExtraAmortization(int dayNr, string creditNr, decimal amount)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var actualAmount = -t.Context.TransactionsQueryable
                    .Where(x => x.CreditNr == creditNr && x.AccountCode == TransactionAccountType.CapitalDebt.ToString()
                        && x.CreditNotificationId == null && x.IncomingPaymentId != null && x.TransactionDate == today)
                    .Sum(x => x.Amount);
                Assert.That(actualAmount, Is.EqualTo(amount), $"{t.Prefix}Extra amortization expected");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectPaidSwedishRseDebt(int dayNr, string creditNr, decimal amount)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var actualAmount = -t.Context.TransactionsQueryable
                    .Where(x => x.CreditNr == creditNr && x.AccountCode == TransactionAccountType.SwedishRseDebt.ToString()
                        && x.CreditNotificationId == null && x.IncomingPaymentId != null && x.TransactionDate == today)
                    .Sum(x => x.Amount);
                Assert.That(actualAmount, Is.EqualTo(amount), $"{t.Prefix}Swedish RSE expected");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectPaidExtraInterest(int dayNr, string creditNr, decimal amount)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var actualAmount = -t.Context.TransactionsQueryable
                    .Where(x => x.CreditNr == creditNr && x.AccountCode == TransactionAccountType.InterestDebt.ToString()
                        && x.CreditNotificationId == null && x.IncomingPaymentId != null && x.TransactionDate == today)
                    .Sum(x => x.Amount);
                Assert.That(actualAmount, Is.EqualTo(amount), $"{t.Prefix}Extra interest expected");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNotificationPartiallyPaid(int dayNr, string creditNr, int dueDay, int? fromMonthNr = null,
            decimal? balanceAfterAmount = null, decimal? interestAfterAmount = null, decimal? capitalAfterAmount = null, decimal? notificationFeeAfterAmount = null,
            (string Code, decimal Amount)? notificationCostAfter = null, decimal? reminderFeeAfterAmount = null, decimal? writtenOffNotNotifiedCapitalAmount = null,
             bool? isClosed = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var monthDelta = t.MonthNr - (fromMonthNr ?? 1);
                var today = t.Support.Clock.Today;
                var dueDate = Month.ContainingDate(monthDelta == 0 ? today : today.AddMonths(-monthDelta)).GetDayDate(dueDay);
                var notificationId = t.Context
                    .CreditNotificationHeadersQueryable
                    .Where(x => x.CreditNr == creditNr && x.DueDate == dueDate)
                    .Select(x => x.Id)
                    .Single();

                var notification = CreditNotificationDomainModel.CreateForSingleNotification(notificationId, t.Context, t.Support.GetRequiredService<PaymentOrderService>().GetPaymentOrderItems());

                if (balanceAfterAmount.HasValue)
                    Assert.That(notification?.GetRemainingBalance(today), Is.EqualTo(balanceAfterAmount.Value), $"{t.Prefix}Notification on {creditNr} remaining amount");

                if (interestAfterAmount.HasValue)
                    Assert.That(notification?.GetRemainingBalance(today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Interest)),
                        Is.EqualTo(interestAfterAmount.Value), $"{t.Prefix}Notification on {creditNr} remaining interest amount");

                if (capitalAfterAmount.HasValue)
                    Assert.That(notification?.GetRemainingBalance(today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital)),
                        Is.EqualTo(capitalAfterAmount.Value), $"{t.Prefix}Notification on {creditNr} remaining capital amount");

                if (notificationFeeAfterAmount.HasValue)
                    Assert.That(notification?.GetRemainingBalance(today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.NotificationFee)),
                        Is.EqualTo(notificationFeeAfterAmount.Value), $"{t.Prefix}Notification on {creditNr} remaining notification fee amount");

                if (notificationCostAfter.HasValue)
                    Assert.That(notification?.GetRemainingBalance(today, PaymentOrderItem.FromCustomCostCode(notificationCostAfter.Value.Code)),
                        Is.EqualTo(notificationCostAfter.Value.Amount), $"{t.Prefix}Notification on {creditNr} remaining {notificationCostAfter.Value.Code} amount");

                if (reminderFeeAfterAmount.HasValue)
                    Assert.That(notification?.GetRemainingBalance(today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.ReminderFee)),
                        Is.EqualTo(reminderFeeAfterAmount.Value), $"{t.Prefix}Notification on {creditNr} remaining reminder fee amount");

                if (writtenOffNotNotifiedCapitalAmount.HasValue)
                {
                    Assert.That(notification?.GetWrittenOffAmount(today, PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital)),
                        Is.EqualTo(writtenOffNotNotifiedCapitalAmount.Value), $"{t.Prefix}Notification on {creditNr} written off capital amount");
                }
                if (isClosed.HasValue)
                    Assert.That(notification?.GetClosedDate(today, onlyUseClosedDate: true).HasValue, Is.EqualTo(isClosed.Value),
                        $"{t.Prefix}Notification on {creditNr} should be {(isClosed.Value ? "Closed" : "Left open")}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNotificationNotPaid(int dayNr, string creditNr, int dueDay, int? fromMonthNr = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var monthDelta = t.MonthNr - (fromMonthNr ?? 1);
                var today = t.Support.Clock.Today;
                var dueDate = Month.ContainingDate(monthDelta == 0 ? today : today.AddMonths(-monthDelta)).GetDayDate(dueDay);
                var notificationId = t.Context
                    .CreditNotificationHeadersQueryable
                    .Where(x => x.CreditNr == creditNr && x.DueDate == dueDate)
                    .Select(x => x.Id)
                    .Single();

                var hasAnyPaymentToday = t.Context.TransactionsQueryable.Any(x => x.CreditNotificationId == notificationId && x.IncomingPaymentId.HasValue && x.TransactionDate == today);
                Assert.That(hasAnyPaymentToday, Is.False, $"{t.Prefix}Notification on {creditNr} due {dueDay} month {fromMonthNr ?? 1} no payment expected");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectedUnplacedBalanceAmount(int dayNr, decimal expectedAmount)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var actualAmount = t.Context.GetConnection().Query<decimal>("select isnull(SUM(amount), 0) from AccountTransaction where AccountCode = 'UnplacedPayment'").Single();
                Assert.That(actualAmount, Is.EqualTo(expectedAmount), $"{t.Prefix}Expected unplaced amount");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectDebtCollectionExport(int dayNr, string creditNr, decimal? reminderFeeAmount = null, decimal? interestAmount = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, t.Context, t.Support.CreditEnvSettings);

                DateTime? debCollectionDate = null;
                Assert.That(credit.GetStatus(t.Support.Clock.Today, observeTransactionDate: x => debCollectionDate = x), Is.EqualTo(CreditStatus.SentToDebtCollection),
                    $"{t.Prefix}Debt collection on {creditNr}");
                Assert.That(debCollectionDate?.ToString("yyyy-MM-dd"), Is.EqualTo(t.Support.CurrentMonth.GetDayDate(dayNr).ToString("yyyy-MM-dd")),
                    $"{t.Prefix}Debt collection on {creditNr}");

                var nonZeroAccountCodes = GetNonZeroAccountCodes(creditNr, t.Context).ToList();
                Assert.That(nonZeroAccountCodes.Count, Is.EqualTo(0), $"{t.Prefix}Debt collection credit {creditNr} has non zero accounts: {string.Join(", ", nonZeroAccountCodes)}");

                Lazy<List<AccountTransaction>> debtCollectionExportTransactions = new Lazy<List<AccountTransaction>>(() => t.Context.TransactionsQueryable
                    .Where(x => x.CreditNr == creditNr && x.BusinessEvent.EventType == BusinessEventType.CreditDebtCollectionExport.ToString()).ToList());
                decimal GetDebtCollectionExportAmount(string accountCode) =>
                    -debtCollectionExportTransactions.Value.Where(x => x.AccountCode == accountCode && x.WriteoffId.HasValue).Sum(x => x.Amount);

                if (reminderFeeAmount.HasValue)
                    Assert.That(GetDebtCollectionExportAmount(TransactionAccountType.ReminderFeeDebt.ToString()), Is.EqualTo(reminderFeeAmount.Value), $"{t.Prefix}Debt collection reminder fee amount");
                if (interestAmount.HasValue)
                    Assert.That(GetDebtCollectionExportAmount(TransactionAccountType.InterestDebt.ToString()), Is.EqualTo(interestAmount.Value), $"{t.Prefix}Debt collection reminder interest amount");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNotShownInDebtCollectionUi(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var s = t.Support.GetRequiredService<DebtCollectionCandidateService>();
                var page = s.GetDebtCollectionCandidatesPage(creditNr, 100, 0, x => x);
                var credit = page.Page.SingleOrDefault(x => x.CreditNr == creditNr);
                Assert.IsNull(credit, $"{t.Prefix}Expected {creditNr} to not be in the debt collection ui");
            });
            return this;

        }
        internal CreditCycleAssertionBuilder ExpectShownInDebtCollectionUi(int dayNr, string creditNr, int debtCollectionExportMonthNr, int debtCollectionExportDayNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var s = t.Support.GetRequiredService<DebtCollectionCandidateService>();
                var page = s.GetDebtCollectionCandidatesPage(creditNr, 100, 0, x => x);
                var credit = page.Page.SingleOrDefault(x => x.CreditNr == creditNr);
                Assert.IsNotNull(credit, $"{t.Prefix}Expected {creditNr} to be in the debt collection ui");
                var expectedDate = t.Support.CurrentMonth.AddMonths(debtCollectionExportMonthNr - t.MonthNr).GetDayDate(debtCollectionExportDayNr);
                Assert.That(credit.WithGraceLatestEligableTerminationLetterDueDate, Is.EqualTo(expectedDate), $"{t.Prefix}Expected {creditNr} to be in termination letter ui with specific candidate date");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNewLoan(int dayNr, string creditNr, decimal? loanAmount = null, decimal? marginInterestRate = null,
            int? singlePaymentRepaymentDays = null, (string Code, decimal Amount)? firstNotificationCost = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, t.Context, t.Support.CreditEnvSettings);
                var today = t.Support.Clock.Today;
                var prefix = $"{t.Prefix} {creditNr}: ";
                Assert.That(credit.GetStartDate().Date, Is.EqualTo(today), $"{prefix}Credit should have been created today");
                if (loanAmount.HasValue)
                    Assert.That(credit.GetBalance(CreditDomainModel.AmountType.Capital, today), Is.EqualTo(loanAmount.Value), $"{prefix}Capital balance");
                if (marginInterestRate.HasValue)
                    Assert.That(credit.GetDatedCreditValue(today, DatedCreditValueCode.MarginInterestRate), Is.EqualTo(marginInterestRate.Value), $"{prefix}Margin interest rate");
                if (singlePaymentRepaymentDays.HasValue)
                    Assert.That((int)credit.GetDatedCreditValue(today, DatedCreditValueCode.SinglePaymentLoanRepaymentDays), Is.EqualTo(singlePaymentRepaymentDays.Value), $"{prefix}Repayment time in days");
                if (firstNotificationCost.HasValue)
                {
                    var costs = credit.GetNotNotifiedNotificationCosts(today);
                    Assert.That(costs.OptS(firstNotificationCost.Value.Code), Is.EqualTo(firstNotificationCost.Value.Amount), $"{prefix}First notification cost");
                }
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectCreditSettled(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var isSettledToday = t.Context.DatedCreditStringsQueryable
                    .Any(x => x.CreditNr == creditNr && x.TransactionDate == today && x.Name == DatedCreditStringCode.CreditStatus.ToString() && x.Value == CreditStatus.Settled.ToString());
                Assert.That(isSettledToday, Is.True, $"{t.Prefix}Credit {creditNr} settled");
                var nonZeroAccountCodes = GetNonZeroAccountCodes(creditNr, t.Context).Where(x => x != "UnplacedPayment").ToList();
                //NOTE: We skip UnplacedPayment since we seem to use CreditNr only when counting that down on payments. That seems wrong but has been so always so for now nothing to change.
                Assert.That(nonZeroAccountCodes.Count, Is.EqualTo(0), $"{t.Prefix}Settled credit {creditNr} has non zero accounts: {string.Join(", ", nonZeroAccountCodes)}");

            });
            return this;
        }

        public CreditCycleAssertionBuilder ExpectAlternatePaymentPlanStarted(int dayNr, string creditNr, decimal? totalAmount, (int MonthNr, int DayNr)? firstDueDate, params decimal[] monthlyAmounts)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var paymentPlan = t.Context.AlternatePaymentPlanHeadersQueryable.Where(x => x.CreatedByEvent.TransactionDate == today && x.CreditNr == creditNr).Select(x => new
                {
                    Months = x.Months.OrderBy(y => y.Id).Select(y => new
                    {
                        y.MonthAmount,
                        y.DueDate
                    }).ToList()
                }).SingleOrDefault();

                Assert.That(paymentPlan, Is.Not.Null, $"{t.Prefix}Expected payment plan on credit {creditNr}");

                if (firstDueDate.HasValue)
                    Assert.That(paymentPlan?.Months[0].DueDate, Is.EqualTo(GetMonth(t, firstDueDate.Value.MonthNr).GetDayDate(firstDueDate.Value.DayNr)), $"{t.Prefix}Payment plan - first due date");

                if (monthlyAmounts?.Length > 0)
                {
                    Assert.That(paymentPlan.Months.Count, Is.EqualTo(monthlyAmounts.Length), $"{t.Prefix}Payment plan length");

                    foreach (var amountPair in monthlyAmounts.Zip(paymentPlan.Months))
                    {
                        Assert.That(amountPair.Second.MonthAmount, Is.EqualTo(amountPair.First), $"{t.Prefix}Invalid monthly amount");
                    }
                }

                if (totalAmount.HasValue)
                    Assert.That(totalAmount.Value, Is.EqualTo(paymentPlan?.Months?.Sum(x => x.MonthAmount)), $"{t.Prefix}Payment plan total amount");
            });
            return this;
        }

        public CreditCycleAssertionBuilder ExpectAlternatePaymentPlanCancelled(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var hasCancelledPlan = t.Context.AlternatePaymentPlanHeadersQueryable.Where(x => x.CreditNr == creditNr && x.CancelledByEvent.TransactionDate == today).Any();
                Assert.That(hasCancelledPlan, Is.EqualTo(true), $"{t.Prefix}Expected cancelled payment plan");
            });
            return this;
        }

        public CreditCycleAssertionBuilder ExpectAlternatePaymentPlanFullyPaid(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var hasFullyPaidPlan = t.Context.AlternatePaymentPlanHeadersQueryable.Where(x => x.CreditNr == creditNr && x.FullyPaidByEvent.TransactionDate == today).Any();
                Assert.That(hasFullyPaidPlan, Is.EqualTo(true), $"{t.Prefix}Expected fully paid payment plan");
            });
            return this;
        }

        private List<string> GetNonZeroAccountCodes(string creditNr, ICreditContextExtended context) => context.GetConnection().Query<string>(@"select	AccountCode
    from	AccountTransaction
    where	CreditNr = @creditNr
    and		TransactionDate <= @date    
    group by AccountCode
    having SUM(amount) <> 0", param: new { creditNr, date = context.CoreClock.Today }).ToList();

        public CreditCycleAssertionBuilder ForMonth(int monthNr)
        {
            forMonthNr = monthNr;
            return this;
        }

        public CreditCycleAssertionBuilder AssertCustom(int dayNr, AssertionType assert)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                assert(t);
            });
            return this;
        }

        internal CreditCycleAssertion End()
        {
            forMonthNr = null;
            return a;
        }

        internal CreditCycleAssertionBuilder ExpectReminder(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var reminder = t
                    .Context
                    .CreditReminderHeadersQueryable
                    .SingleOrDefault(y => y.TransactionDate == today && y.CreditNr == creditNr);
                Assert.That(reminder, Is.Not.Null, $"{t.Prefix}Reminder on {creditNr}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectTerminationLetter(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var terminationLetter = t
                    .Context
                    .CreditTerminationLetterHeadersQueryable
                    .SingleOrDefault(y => y.TransactionDate == today && y.CreditNr == creditNr);
                Assert.That(terminationLetter, Is.Not.Null, $"{t.Prefix}Termination letter on {creditNr}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectTerminationLetterLifted(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var wasLiftedToday = t
                    .Context
                    .CreditTerminationLetterHeadersQueryable
                    .Any(y => y.InactivatedByBusinessEvent.TransactionDate == today && y.CreditNr == creditNr);
                Assert.That(wasLiftedToday, Is.True, $"{t.Prefix}Termination letter lifted on {creditNr}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectTerminationLetterPaused(int dayNr, string creditNr, (int MonthNr, int DayNr)? pausedUntil = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var actuallyPausedUntilDate = t.Context
                    .DatedCreditDatesQueryable.Where(x => x.CreditNr == creditNr && x.TransactionDate == today && x.Name == DatedCreditDateCode.TerminationLettersPausedUntilDate.ToString())
                    .Select(x => (DateTime?)x.Value)
                    .FirstOrDefault();
                Assert.That(actuallyPausedUntilDate.HasValue, Is.True, $"{t.Prefix}Expected termination letter paused {creditNr}");
                if (pausedUntil.HasValue)
                {
                    var expectedPausedUntilDate = GetMonth(t, pausedUntil.Value.MonthNr).GetDayDate(pausedUntil.Value.DayNr);
                    Assert.That(actuallyPausedUntilDate.Value, Is.EqualTo(expectedPausedUntilDate), $"{t.Prefix}Expected termination letter paused {creditNr}");
                }
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNotShownInTerminationLetterUi(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var s = t.Support.GetRequiredService<TerminationLetterCandidateService>();
                var page = s.GetTerminationLetterCandidatesPage(100, 0, creditNr, x => x);
                var credit = page.Page.SingleOrDefault(x => x.CreditNr == creditNr);
                Assert.IsNull(credit, $"{t.Prefix}Expected {creditNr} to not be in the termination letter ui");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectShownInTerminationLetterUi(int dayNr, string creditNr, int letterSentMonthNr, int letterSentDayNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var s = t.Support.GetRequiredService<TerminationLetterCandidateService>();
                var page = s.GetTerminationLetterCandidatesPage(100, 0, creditNr, x => x);
                var credit = page.Page.SingleOrDefault(x => x.CreditNr == creditNr);
                Assert.IsNotNull(credit, $"{t.Prefix}Expected {creditNr} to be in the termination letter ui");
                var expectedDate = t.Support.CurrentMonth.AddMonths(letterSentMonthNr - t.MonthNr).GetDayDate(letterSentDayNr);
                Assert.That(credit.TerminationCandidateDate, Is.EqualTo(expectedDate), $"{t.Prefix}Expected {creditNr} to be in the termination letter ui with specific candidate date");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectScheduledDirectDebitPayment(int dayNr, string creditNr, decimal amount)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var tomorrow = t.Support.Clock.Today.AddDays(1);
                var eventType = $"BusinessEvent_{BusinessEventType.ScheduledDirectDebitPayment}";

                var directDebitCommentData = t
                    .Context
                    .CreditCommentsQueryable
                    .Where(x => x.CreditNr == creditNr && x.EventType == eventType && x.CommentDate >= today && x.CommentDate < tomorrow)
                    .Select(x => x.Attachment)
                    .SingleOrDefault();

                var directDebitData = DirectDebitNotificationDeliveryService.DirectDebitPaymentLogData.FromCommentAttachment(CreditCommentAttachmentModel.Parse(directDebitCommentData));
                Assert.That(directDebitData, Is.Not.Null, $"{t.Prefix}Direct debit scheduled payment on {creditNr}");
                Assert.That(directDebitData.Amount, Is.EqualTo(amount), $"{t.Prefix}Direct debit scheduled payment on {creditNr}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNoScheduledDirectDebitPayment(int dayNr, string creditNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var tomorrow = t.Support.Clock.Today.AddDays(1);
                var eventType = $"BusinessEvent_{BusinessEventType.ScheduledDirectDebitPayment}";

                var hasDirectDebitCommentData = t
                    .Context
                    .CreditCommentsQueryable
                    .Any(x => x.CreditNr == creditNr && x.EventType == eventType && x.CommentDate >= today && x.CommentDate < tomorrow);
                Assert.That(hasDirectDebitCommentData, Is.False, $"{t.Prefix}No direct debit scheduled payment on {creditNr}");
            });
            return this;
        }

        private Month GetMonth((ICreditContextExtended Context, CreditSupportShared Support, int MonthNr, int DayNr, string Prefix) t, int monthNr) =>
            t.Support.CurrentMonth.AddMonths(monthNr - t.MonthNr);

        internal CreditCycleAssertionBuilder ExpectPaymentFreeMonthRequested(int dayNr, string creditNr, int paymentFreeMonthNr)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var expectedMonthDate = GetMonth(t, paymentFreeMonthNr).GetDayDate(1);
                var today = t.Support.Clock.Today;
                var hasFutureMonth = t.Context.CreditFuturePaymentFreeMonthsQueryable
                    .Where(x => x.CreditNr == creditNr && x.CreatedByEvent.TransactionDate == today && x.ForMonth == expectedMonthDate).Any();
                Assert.That(hasFutureMonth, Is.EqualTo(true), $"{t.Prefix}Expected future payment free month for month {paymentFreeMonthNr} ({expectedMonthDate.ToString("yyyy-MM")})");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectPaymentFreeMonth(int dayNr, string creditNr, int dueDay)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var today = t.Support.Clock.Today;
                var expectedDueDate = t.Support.CurrentMonth.GetDayDate(dueDay);
                var hasPaymentFreeMonthMonth = t.Context.CreditPaymentFreeMonthsQueryable
                    .Where(x => x.CreditNr == creditNr && x.CreatedByEvent.TransactionDate == today && x.DueDate == expectedDueDate).Any();
                Assert.That(hasPaymentFreeMonthMonth, Is.EqualTo(true), $"{t.Prefix}Expected payment free month due {expectedDueDate.ToString("yyyy-MM-dd")}");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectNrOfFutureNotifications(int dayNr, string creditNr, int count)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var p = Credits.GetCurrentPaymentPlan(t.Support, creditNr);
                Assert.That(p.Payments.Count, Is.EqualTo(count), $"{t.Prefix}Nr of future notifications");
            });
            return this;
        }

        internal CreditCycleAssertionBuilder ExpectLoanBalance(int dayNr, string creditNr, decimal? notNotifiedCapitalAmount = null, decimal? capitalAmount = null)
        {
            a.AddAssertion(forMonthNr, dayNr, t =>
            {
                var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, t.Context, t.Support.CreditEnvSettings);

                if (notNotifiedCapitalAmount.HasValue)
                    Assert.That(credit.GetNotNotifiedCapitalBalance(t.Support.Clock.Today),
                        Is.EqualTo(notNotifiedCapitalAmount.Value), $"{t.Prefix}Not notified capital balance on credit {creditNr}");

                if (capitalAmount.HasValue)
                    Assert.That(credit.GetBalance(CreditDomainModel.AmountType.Capital, t.Support.Clock.Today),
                        Is.EqualTo(capitalAmount.Value), $"{t.Prefix}Capital balance on credit {creditNr}");
            });
            return this;
        }

        internal class CreditCycleAssertion
        {
            public Dictionary<int, Dictionary<int, List<AssertionType>>> assertionByMonthNrAndDayNr = new Dictionary<int, Dictionary<int, List<AssertionType>>>();

            public int MaxMonthNr => assertionByMonthNrAndDayNr.Keys.Count == 0 ? 1 : assertionByMonthNrAndDayNr.Keys.Max();

            public void AddAssertion(int? monthNrPre, int dayNr, AssertionType a)
            {
                var monthNr = monthNrPre!.Value;
                if (!assertionByMonthNrAndDayNr.ContainsKey(monthNr))
                    assertionByMonthNrAndDayNr.Add(monthNr, new Dictionary<int, List<AssertionType>>());

                var month = assertionByMonthNrAndDayNr[monthNr];

                if (!month.ContainsKey(dayNr))
                    month[dayNr] = new List<AssertionType>();

                month[dayNr].Add(a);
            }

            public void DoAssert(CreditSupportShared support, int monthNr, int dayNr)
            {
                var assertions = assertionByMonthNrAndDayNr?.Opt(monthNr)?.Opt(dayNr);
                if (assertions == null || assertions.Count == 0)
                    return;

                using (var context = support.CreateCreditContextFactory().CreateContext())
                {
                    foreach (var assertion in assertions)
                        assertion((Context: context, Support: support, MonthNr: monthNr, DayNr: dayNr, Prefix: $"monthNr={monthNr}, dayNr={dayNr}: "));
                }
            }
        }
    }
}