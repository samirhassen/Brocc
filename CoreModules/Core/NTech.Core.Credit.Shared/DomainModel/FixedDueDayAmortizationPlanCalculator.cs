using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static nCredit.DbModel.DomainModel.NotificationProcessSettings;

namespace nCredit
{
    public static class FixedDueDayAmortizationPlanCalculator
    {
        public static bool TryGetAmortizationPlan(HistoricalCreditModel c, NotificationProcessSettings processSettings, out AmortizationPlan p, out string failedMessage, ICoreClock clock, Func<DateTime, decimal> getDividerOverride, Action<DateTime, decimal> observeDailyInterestAmount = null)
        {
            if (c.NotificationDueDay.HasValue)
                throw new Exception("Does not support per loan due dates");

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

            var items = new List<AmortizationPlan.Item>();

            //--------------------------------------------------------
            //--------------Historical events ------------------------
            //--------------------------------------------------------
            var historicalMonths = PerLoanDueDayAmortizationPlanCalculator.GetHistoricMonths(c);
            items.AddRange(historicalMonths.Items);
            var capitalDebt = historicalMonths.CapitalDebt;

            if (c.ProcessSuspendingTerminationLetters != null && c.ProcessSuspendingTerminationLetters.Count > 0)
            {
                items = ExtendHistoricalItemsWithTerminationLetters(items, c.ProcessSuspendingTerminationLetters);
            }

            //Future months
            var lastDueDate = c.Transactions.Where(x => x.CreditNotificationDueDate.HasValue).Select(x => (DateTime?)x.CreditNotificationDueDate.Value).Max();
            var lastNotificationFreeMonthDate = c.Transactions.Where(x => x.PaymentFreeMonthDueDate.HasValue).Select(x => (DateTime?)x.PaymentFreeMonthDueDate.Value).Max();

            var creditCreatedDate = c.CreatedByEvent.TransactionDate;

            var interestStartDate = GetFutureNotificationsInterestStartDate(lastDueDate, lastNotificationFreeMonthDate, creditCreatedDate);
            var nextNotificationMonth = GetNextNotificationMonth(processSettings, clock, lastDueDate, lastNotificationFreeMonthDate);

            if (!c.MarginInterestRatePercent.HasValue)
            {
                p = null;
                failedMessage = $"Missing margin interest rate for credit {c.CreditNr}";
                return false;
            }

            decimal interestRatePercent = c.ReferenceInterestRatePercent + c.MarginInterestRatePercent.Value;

            List<AmortizationPlan.Item> futureItems;
            if (!TrySimulateFutureMonths(capitalDebt, nextNotificationMonth, interestStartDate, interestRatePercent, c.AmortizationModel, c.NotificationFee, c.PendingFuturePaymentFreeMonths?.Select(x => x.ForMonth)?.ToList() ?? new List<DateTime>(), processSettings, c.EndDate, out futureItems, out failedMessage, getDividerOverride, observeDailyInterestAmount: observeDailyInterestAmount))
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

        public static bool TrySimulateFutureMonths(decimal capitalDebt, DateTime nextNotificationMonth, DateTime interestStartDate, decimal interestRatePercent, CreditAmortizationModel amortizationModel, decimal notificationFeeAmount, List<DateTime> futurePaymentFreeMonths, NotificationProcessSettings processSettings, DateTime? endDate, out List<AmortizationPlan.Item> futureMonths, out string failedMessage, Func<DateTime, decimal> getDividerOverride, Action<DateTime, decimal> observeDailyInterestAmount = null, int? onlyComputeFirstNMonths = null)
        {
            var items = new List<AmortizationPlan.Item>();
            int guard = 0;
            int monthNr = 1;
            while (capitalDebt > 0m && guard++ < 10000)
            {
                if (onlyComputeFirstNMonths.HasValue && monthNr > onlyComputeFirstNMonths.Value)
                {
                    break;
                }
                var notificationDate = new DateTime(nextNotificationMonth.Year, nextNotificationMonth.Month, processSettings.NotificationNotificationDay);
                var dueDate = new DateTime(nextNotificationMonth.Year, nextNotificationMonth.Month, processSettings.NotificationDueDay);

                var isPaymentFree = futurePaymentFreeMonths.Any(x => x.Year == notificationDate.Year && x.Month == notificationDate.Month);

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
                nextNotificationMonth = dueDate.AddMonths(1);
                interestStartDate = dueDate.AddDays(1);
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

        private static DateTime GetNextNotificationMonth(IAmortizationPlanNotificationSettings processSettings, ICoreClock clock, DateTime? lastDueDate, DateTime? lastNotificationFreeMonthDate) =>
            GetNextNotificationMonth(processSettings, clock.Today, lastDueDate, lastNotificationFreeMonthDate);

        private static DateTime GetNextNotificationMonth(IAmortizationPlanNotificationSettings processSettings, DateTime today, DateTime? lastDueDate, DateTime? lastNotificationFreeMonthDate)
        {
            var nextNotificationMonthAfterToday = new DateTime(today.Year, today.Month, processSettings.NotificationNotificationDay)
                .AddMonths(today.Day >= processSettings.NotificationNotificationDay ? 1 : 0);

            if (lastDueDate.HasValue || lastNotificationFreeMonthDate.HasValue)
            {
                var nextNotificationMonthBasedOnHistory = MaxDate(lastDueDate, lastNotificationFreeMonthDate).AddMonths(1);
                return nextNotificationMonthBasedOnHistory < today ? nextNotificationMonthAfterToday : nextNotificationMonthBasedOnHistory;
            }
            else
            {
                return nextNotificationMonthAfterToday;
            }
        }

        private static DateTime GetFutureNotificationsInterestStartDate(DateTime? lastDueDate, DateTime? lastNotificationFreeMonthDate, DateTime creditCreatedDate)
        {
            return lastDueDate.HasValue || lastNotificationFreeMonthDate.HasValue
                ? MaxDate(lastDueDate, lastNotificationFreeMonthDate).AddDays(1)
                : creditCreatedDate;
        }

        private static List<AmortizationPlan.Item> ExtendHistoricalItemsWithTerminationLetters(List<AmortizationPlan.Item> items, List<HistoricalCreditModel.ProcessSuspendingTerminationLetter> terminationLetters)
        {
            /*
                We have times in the history where termination letters have been created and inactivated and we want to add those events
                to the timeline to help the user understand why there might be gaps in the notification history.

                So say there is a history that looks like this:
                2022-03-29: NewCredit
                2022-04-14: NewNotification
                2022-05-14: NewNotification
                2022-06-14: NewNotification
                2022-07-14: NewNotification
                2022-09-14: NewNotification

                And a termination letters was created 2022-07-14 and later inactivated on 2022-08-20.

                We want the timeline to look like this:
                2022-03-29: NewCredit
                2022-04-14: NewNotification
                2022-05-14: NewNotification
                2022-06-14: NewNotification
                2022-07-14: NewNotification
                2022-07-14: TerminationLetterProcessSuspension
                2022-08-20: TerminationLetterProcessReactivation
                2022-09-14: NewNotification

                Which then explains why there is no notification on 2022-08-14
                We inject these new events in the right place by numbering all of the real rows starting from 1 each day (so in the above example all historial items have DayOrderNr = 1)
                We then give each suspension an even larger nr so they end up last on each day
                Reactivations are placed after suspensions since the most likely case where this matter is when the user creates and removes a letter on the same day
                since they found one that was sent that was by accident. We then dont want the re-activation to be before the suspension.                 
                      
                Then just sort this new aggregate list by date and then DayOrderNr.
             */

            var orderNrPerDate = new Dictionary<DateTime, int>();
            int GetNextDayOrderNr(DateTime d) => orderNrPerDate.AddOrUpdate(d, 1, x => x + 1);
            var orderedItems = items.Select(x => new { DayOrderNr = GetNextDayOrderNr(x.EventTransactionDate), Item = x }).ToList();
            var placeLastDayOrderNr = orderNrPerDate.Values.Max() + 1;
            var resultItems = new List<AmortizationPlan.Item>(
                orderedItems
                .Concat(terminationLetters.Select(x => new
                {
                    DayOrderNr = placeLastDayOrderNr,
                    Item = new AmortizationPlan.Item
                    {
                        EventTransactionDate = x.SuspendedFromDate,
                        IsTerminationLetterProcessSuspension = true,
                        EventTypeCode = "TerminationLetterProcessSuspension"
                    }
                }))
                .Concat(terminationLetters.Where(x => x.SuspendedToDate.HasValue).Select(x => new
                {
                    DayOrderNr = placeLastDayOrderNr + 1,
                    Item = new AmortizationPlan.Item
                    {
                        EventTransactionDate = x.SuspendedToDate.Value,
                        IsTerminationLetterProcessReActivation = true,
                        EventTypeCode = "TerminationLetterProcessReactivation"
                    }
                }))
                .OrderBy(x => x.Item.EventTransactionDate).ThenBy(x => x.DayOrderNr).Select(x => x.Item));

            //Set CapitalBefore on the new fake items to the same as the previous item
            //Probably not super important but helps consumers that dont know about these item types not mess up running totals or similar
            for (var i = 1; i < resultItems.Count; i++) //Start at 1 since even if 0 is one there is nothing to copy from
            {
                var item = resultItems[i];
                if (item.IsTerminationLetterProcessReActivation || item.IsTerminationLetterProcessSuspension)
                {
                    item.CapitalBefore = resultItems[i - 1].CapitalBefore;
                }
            }

            return resultItems;
        }

        public static DateTime? CalculateEndDateForFixedPaymentMortgageLoan(
            DateTime? lastDueDate,
            decimal monthlyFixedCapitalAmount,
            DateTime? endDate,
            decimal notNotifiedCapitalAmountAfterLastNotification,
            DateTime? amortizationExceptionUntilDate, 
            decimal? amortizationExceptionAmount,
            DateTime today,
            IAmortizationPlanNotificationSettings notificationProcessSettings,
            DateTime? closedDate,
            Action<DateTime> observeNextDueDate = null)
        {
            if (notNotifiedCapitalAmountAfterLastNotification <= 0m && lastDueDate.HasValue)
                return lastDueDate.Value; //Overdue on the last notification
            
            if (closedDate.HasValue)
                return null; //Or possibly throw. We would need to know the last payment date here
            
            if (notNotifiedCapitalAmountAfterLastNotification <= 0m)
                return null; //Already zeroed out somehow at an unknown date

            if (monthlyFixedCapitalAmount <= 0m)
                return Month.ContainingDate(endDate.Value).GetDayDate(notificationProcessSettings.NotificationDueDay);

            var amortizationModel = CreditAmortizationModel.CreateMonthlyFixedCapitalAmount(monthlyFixedCapitalAmount, null, amortizationExceptionUntilDate, amortizationExceptionAmount);

            var notificationMonth = Month.ContainingDate(GetNextNotificationMonth(notificationProcessSettings, today, lastDueDate, null));

            var capitalDebt = notNotifiedCapitalAmountAfterLastNotification;
            var notificationDate = notificationMonth.GetDayDate(notificationProcessSettings.NotificationNotificationDay);
            var dueDate = notificationMonth.GetDayDate(notificationProcessSettings.NotificationDueDay);

            bool isNextDueDateObserved = false;
            int guard = 0;
            while (guard++ < 10000)
            {
                if (!isNextDueDateObserved)
                {
                    observeNextDueDate?.Invoke(dueDate);
                    isNextDueDateObserved = true;
                }                

                if (endDate.HasValue && endDate.Value < dueDate)
                {
                    return dueDate;
                }                    

                var capitalAmount = amortizationModel.GetNotificationCapitalAmount(notificationDate, dueDate, 0m); //NOTE: Interest amount doesnt matter for fixed amortization

                if (capitalAmount < 0m)
                    capitalAmount = 0;

                if (capitalAmount > capitalDebt)
                    capitalAmount = capitalDebt;
                else if (amortizationModel.ShouldCarryOverRemainingCapitalAmount(notificationDate, dueDate, capitalDebt - capitalAmount, PaymentPlanCalculation.DefaultSettings) || (endDate.HasValue && endDate.Value.Year == dueDate.Year && endDate.Value.Month == dueDate.Month))
                    capitalAmount = capitalDebt;

                capitalDebt -= capitalAmount;

                if (capitalDebt <= 0)
                    return dueDate;

                notificationMonth = notificationMonth.NextMonth;
                notificationDate = notificationMonth.GetDayDate(notificationProcessSettings.NotificationNotificationDay);
                dueDate = notificationMonth.GetDayDate(notificationProcessSettings.NotificationDueDay);
            }
            throw new NTechCoreWebserviceException("Hit infinite loop guard. Something is wrong with this logic.");
        }

        private static DateTime MaxDate(DateTime? d1, DateTime? d2)
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
        }
    }
}