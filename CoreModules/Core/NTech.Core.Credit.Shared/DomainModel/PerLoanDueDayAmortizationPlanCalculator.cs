using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit
{
    public static class PerLoanDueDayAmortizationPlanCalculator
    {
        public static (List<AmortizationPlan.Item> Items, decimal CapitalDebt) GetHistoricMonths(HistoricalCreditModel c)
        {
            var items = new List<AmortizationPlan.Item>();

            decimal? GetSum(IEnumerable<HistoricalCreditModel.ModelTransaction> ts, TransactionAccountType t)
            {
                var m = ts.Where(x => x.AccountCode == t.ToString());
                if (m.Any())
                    return m.Sum(x => (decimal?)x.Amount);
                else
                    return null;
            }

            var capitalDebt = 0m;
            AmortizationPlan.Item GetHistoricBaseItem(IEnumerable<HistoricalCreditModel.ModelTransaction> ts, HistoricalCreditModel.ModelBusinessEvent e, string businessEventRoleCode, bool isWriteOff)
            {
                var i = new AmortizationPlan.Item
                {
                    CapitalBefore = capitalDebt,
                    CapitalTransaction = -(GetSum(ts.Where(x => x.IsWriteOff == isWriteOff), TransactionAccountType.NotNotifiedCapital) ?? 0m),
                    EventTransactionDate = e.TransactionDate,
                    EventTypeCode = e.EventType,
                    IsWriteOff = isWriteOff,
                    //Only include created interest debt
                    InterestTransaction = isWriteOff
                        ? null
                        : GetSum(ts.Where(x => !x.IsIncomingPayment && !x.IsWriteOff), TransactionAccountType.InterestDebt),
                    BusinessEventRoleCode = businessEventRoleCode
                };
                i.TotalTransaction = i.CapitalTransaction + (i.InterestTransaction ?? 0m);
                capitalDebt -= i.CapitalTransaction;
                return i;
            }
            void AddIfNonZero(AmortizationPlan.Item i)
            {
                if (i.CapitalTransaction != 0m || i.InterestTransaction.HasValue)
                {
                    items.Add(i);
                }
            }

            foreach (var evt in c.Transactions
                .GroupBy(x => new { x.BusinessEvent.Id, x.BusinessEventRoleCode })
                .Select(x => new
                {
                    Event = x.First().BusinessEvent,
                    BusinessEventRoleCode = x.First().BusinessEventRoleCode,
                    Transactions = x.ToList()
                })
                .OrderBy(x => x.Event.TransactionDate)
                .ThenBy(x => x.Event.Timestamp, new NaturalOrderByteArrayComparer()))
            {
                AddIfNonZero(GetHistoricBaseItem(evt.Transactions, evt.Event, evt.BusinessEventRoleCode, true));
                AddIfNonZero(GetHistoricBaseItem(evt.Transactions, evt.Event, evt.BusinessEventRoleCode, false));
            }

            return (Items: items, CapitalDebt: capitalDebt);
        }

        public static bool TryGetAmortizationPlan(HistoricalCreditModel c, NotificationProcessSettings processSettings, out AmortizationPlan p, out string failedMessage, ICoreClock clock, IClientConfigurationCore clientConfiguration, Func<DateTime, decimal> getDividerOverride, Action<DateTime, decimal> observeDailyInterestAmount = null)
        {
            if (NewCreditTerminationLettersBusinessEventManager.HasTerminationLettersThatSuspendTheCreditProcess(clientConfiguration))
                throw new Exception("This is not reliable with process suspensions in place. If this is needed, try to merge this back into FixedDueDayAmortizationPlanCalculator.");

            if (c == null)
            {
                p = null;
                failedMessage = "No such credit";
                return false;
            }

            if (c.Status != CreditStatus.Normal.ToString())
            {
                p = null;
                failedMessage = $"Credit is {c.Status}";
                return false;
            }

            //Only open credits
            var items = new List<AmortizationPlan.Item>();

            //--------------------------------------------------------
            //--------------Historical events ------------------------
            //--------------------------------------------------------
            var historicalMonths = GetHistoricMonths(c);
            items.AddRange(historicalMonths.Items);
            var capitalDebt = historicalMonths.CapitalDebt;

            //Future months
            var lastDueDate = c.Transactions.Where(x => x.CreditNotificationDueDate.HasValue).Select(x => (DateTime?)x.CreditNotificationDueDate.Value).Max();
            var lastNotificationFreeMonthDate = c.Transactions.Where(x => x.PaymentFreeMonthDueDate.HasValue).Select(x => (DateTime?)x.PaymentFreeMonthDueDate.Value).Max();

            var creditCreatedDate = c.CreatedByEvent.TransactionDate;

            Func<DateTime?, DateTime?, DateTime> maxD = (d1, d2) =>
            {
                if (d1.HasValue && d2.HasValue)
                {
                    if (d1.Value > d2.Value)
                        return d1.Value;
                    else
                        return d2.Value;
                }
                else if (d1.HasValue)
                    return d1.Value;
                else
                    return d2.Value;
            };

            DateTime interestStartDate;
            DateTime nextNotificationMonth;
            Func<DateTime, Tuple<DateTime, DateTime>> getNextPerLoanDates = getNextPerLoanDates = CreateNextNotificationAndDueDateFromStartDateFactory(c.CreditNr, new NotificationService.CreditNotificationStatusCommon
            {
                CreditStatus = c.Status,
                SinglePaymentLoanRepaymentDays = c.SinglePaymentLoanRepaymentDays,
                CreditNr = c.CreditNr,
                CreditStartDate = c.CreatedByEvent.TransactionDate,
                LatestNotificationDueDate = lastDueDate,
                LatestPaymentFreeMonthDueDate = lastNotificationFreeMonthDate,
                PerLoanDueDay = c.NotificationDueDay.Value,
                IsMissingNotNotifiedCapital = capitalDebt <= 0m
            }, processSettings);

            if (lastDueDate.HasValue || lastNotificationFreeMonthDate.HasValue)
            {
                var lastDate = maxD(lastDueDate, lastNotificationFreeMonthDate.HasValue ? new DateTime(lastNotificationFreeMonthDate.Value.Year, lastNotificationFreeMonthDate.Value.Month, c.NotificationDueDay.Value) : new DateTime?());
                interestStartDate = lastDate.AddDays(1);
                nextNotificationMonth = getNextPerLoanDates(lastDate).Item1;
            }
            else
            {
                var today = clock.Today;
                interestStartDate = creditCreatedDate;
                nextNotificationMonth = getNextPerLoanDates(clock.Today).Item1;
            }

            if (!c.MarginInterestRatePercent.HasValue)
            {
                p = null;
                failedMessage = $"Missing margin interest rate for credit {c.CreditNr}";
                return false;
            }

            decimal interestRatePercent = c.ReferenceInterestRatePercent + c.MarginInterestRatePercent.Value;

            var paymentFreeDueDates = (c.PendingFuturePaymentFreeMonths ?? new List<HistoricalCreditModel.PendingFuturePaymentFreeMonthModel>())
                .Select(x => new DateTime(x.ForMonth.Year, x.ForMonth.Month, c.NotificationDueDay.Value)).ToHashSetShared();
            List<AmortizationPlan.Item> futureItems;
            if (!TrySimulateFutureMonths(capitalDebt, nextNotificationMonth, interestStartDate, interestRatePercent, c.AmortizationModel, c.NotificationFee, paymentFreeDueDates.Contains, c.EndDate, getNextPerLoanDates, out futureItems, out failedMessage, getDividerOverride, observeDailyInterestAmount: observeDailyInterestAmount))
            {
                p = null;
                return false;
            }

            items.AddRange(futureItems);
            p = new AmortizationPlan
            {
                CurrentCapitalDebt = capitalDebt,
                AmortizationModel = c.AmortizationModel,
                NotificationFee = c.NotificationFee,
                NrOfRemainingPayments = items.Where(x => x.IsFutureItem).Count(),
                Items = items
            };
            failedMessage = null;
            return true;
        }

        public static Func<DateTime, Tuple<DateTime, DateTime>> CreateNextNotificationAndDueDateFromStartDateFactory(string context,
            NotificationService.CreditNotificationStatusCommon status,
            NotificationProcessSettings processSettings)
        {
            return startDate =>
            {
                var today = startDate;
                var guard = 0;
                while (guard++ < 90)
                {
                    var skipReason = NotificationService.GetNotificationDueDateOrSkipReason(status, today, status.PerLoanDueDay.HasValue ? new int?() : processSettings.NotificationNotificationDay);
                    if (skipReason.Item1)
                        return Tuple.Create(today, skipReason.Item2.Value);
                    today = today.AddDays(1);
                }
                throw new Exception($"{context}: Could not find a possible notification date within 90 days. Something is broken.");
            };
        }

        public class PossiblePaymentFreeItem
        {
            public bool? IsPossible { get; set; }
            public AmortizationPlan.Item Item { get; set; }
        }

        public static bool TrySimulateFutureMonths(decimal capitalDebt,
            DateTime firstNotificationMonth,
            DateTime interestStartDate,
            decimal interestRatePercent,
            CreditAmortizationModel amortizationModel,
            decimal notificationFeeAmount,
            Func<DateTime, bool> isPaymentFreeDueDate,
            DateTime? endDate,
            Func<DateTime, Tuple<DateTime, DateTime>> getNextPerLoanDates,
            out List<AmortizationPlan.Item> futureMonths,
            out string failedMessage,
            Func<DateTime, decimal> getDividerOverride,
            Action<DateTime, decimal> observeDailyInterestAmount = null)
        {
            var items = new List<AmortizationPlan.Item>();
            int guard = 0;
            int monthNr = 1;
            Tuple<DateTime, DateTime> nextDates = getNextPerLoanDates(firstNotificationMonth);
            while (capitalDebt > 0m && guard++ < 10000)
            {
                var notificationDate = nextDates.Item1;
                var dueDate = nextDates.Item2;

                var isPaymentFree = isPaymentFreeDueDate(dueDate);

                var localDebt = capitalDebt;
                int iDays;
                var interestAmount = CreditDomainModel.ComputeInterestBetweenDays(interestStartDate, dueDate, _ => localDebt, _ => interestRatePercent, null, null, out iDays, getDividerOverride, observeDailyInterestAmount: observeDailyInterestAmount);
                var capitalAmount = amortizationModel.GetNotificationCapitalAmount(notificationDate, dueDate, interestAmount);

                if (capitalAmount < 0m)
                {
                    capitalAmount = 0;
                }

                if (capitalAmount > capitalDebt)
                {
                    capitalAmount = capitalDebt;
                }
                else if (amortizationModel.ShouldCarryOverRemainingCapitalAmount(notificationDate, dueDate, capitalDebt - capitalAmount, PaymentPlanCalculation.DefaultSettings) || (endDate.HasValue && endDate.Value.Year == dueDate.Year && endDate.Value.Month == dueDate.Month))
                {
                    capitalAmount = capitalDebt;
                }
                var notificationItem = new AmortizationPlan.Item
                {
                    CapitalBefore = capitalDebt,
                    CapitalTransaction = capitalAmount,
                    InterestTransaction = interestAmount,
                    NotificationFeeTransaction = notificationFeeAmount,
                    TotalTransaction = capitalAmount + interestAmount, //Not including notificationfee atm. Should probably move this super whack logic to the ui
                    EventTransactionDate = notificationDate,
                    EventTypeCode = BusinessEventType.NewNotification.ToString(),
                    IsFutureItem = true,
                    FutureItemDueDate = dueDate
                };
                AmortizationPlan.Item actualItem;
                if (!isPaymentFree)
                {
                    actualItem = notificationItem;
                }
                else
                {
                    var amount = -(notificationItem.InterestTransaction.GetValueOrDefault() + notificationItem.NotificationFeeTransaction);
                    actualItem = new AmortizationPlan.Item
                    {
                        CapitalBefore = capitalDebt,
                        CapitalTransaction = amount,
                        InterestTransaction = 0m,
                        TotalTransaction = amount,
                        EventTransactionDate = notificationDate,
                        EventTypeCode = BusinessEventType.PaymentFreeMonth.ToString(),
                        IsFutureItem = true,
                        FutureItemDueDate = dueDate
                    };
                }

                items.Add(actualItem);
                capitalDebt -= actualItem.CapitalTransaction;
                interestStartDate = dueDate.AddDays(1);
                nextDates = getNextPerLoanDates(dueDate.AddDays(1));
                monthNr++;
            }
            if (guard > 9000)
            {
                futureMonths = null;
                failedMessage = "Will never be paid with current terms";
                return false;
            }

            futureMonths = items;
            failedMessage = null;
            return true;
        }
    }
}