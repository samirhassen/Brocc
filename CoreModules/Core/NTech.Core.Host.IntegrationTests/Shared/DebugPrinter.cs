using nCredit;
using nCredit.Code.Uc.CreditRegistry;
using nCredit.DomainModel;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.Shared
{
    public static class DebugPrinter
    {
        public static void PrintMonthStart(SupportShared support)
        {
            TestContext.WriteLine($"{support.Now.ToString("yyyy-MM")}");
            TestContext.WriteLine("---------------------------");
        }

        public static void PrintMonthEnd()
        {
            TestContext.WriteLine();
        }

        public static void PrintDay<TSupport>(TSupport support, DateTime date) where TSupport : SupportShared, ISupportSharedCredit
        {
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                var credits = context.CreditHeaders.Where(x => x.CreatedByEvent.TransactionDate <= date).Select(x => new
                {
                    x.CreditNr,
                    x.CreatedByEvent.TransactionDate
                }).ToList();

                void PrintCredit(string creditNr, string eventText) => TestContext.WriteLine($" {date:dd}: {eventText} [{creditNr}]");

                foreach (var credit in credits)
                {
                    if (credit.TransactionDate == date)
                    {
                        var description = DescribeCreditCreated(context, credit.CreditNr, date, support.CreditEnvSettings);
                        PrintCredit(credit.CreditNr, description);
                    }
                    var dayEvents = DescribeCreditDay(support, date, context, credit.CreditNr);
                    foreach (var evt in dayEvents)
                    {
                        PrintCredit(credit.CreditNr, evt);
                    }
                }
            }
        }

        private static string DescribeCreditCreated(CreditContextExtended context, string creditNr, DateTime date, ICreditEnvSettings envSettings)
        {
            var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);
            var a = credit.GetAmortizationModel(date);
            var amortizationDescription = a.UsingCurrentAnnuityOrFixedMonthlyCapital(date, annuity => $"Annuity={annuity}", monthlyAmortization => $"MonthlyAmortization={monthlyAmortization}");
            

            return $"Credit created Capital={credit.GetNotNotifiedCapitalBalance(date)} {amortizationDescription} Interest={credit.GetInterestRatePercent(date)} NotificationFee={credit.GetNotificationFee(date)}";
        }

        private static List<string> DescribeCreditDay<TSupport>(TSupport support, DateTime date, CreditContextExtended context, string creditNr) where TSupport : SupportShared, ISupportSharedCredit
        {
            var events = new List<string?>();

            var paymentOrderService = support.GetRequiredService<PaymentOrderService>();
            events.Add(DescribePayments(context, date, creditNr));
            events.AddRange(DescribeNotifications(context, date, creditNr, paymentOrderService));
            events.AddRange(DescribeReminders(context, date, creditNr));
            events.AddRange(DescribeTerminationLetters(context, date, creditNr));
            events.AddRange(DescribeDebtCollection(context, date, creditNr));

            return events.Where(x => x != null).ToList()!;
        }

        private static string? DescribePayments(CreditContextExtended context, DateTime date, string creditNr)
        {
            var paidAmount = -context
                .Transactions
                .Where(x => x.TransactionDate == date && x.CreditNr == creditNr && x.IncomingPaymentId != null && x.WriteoffId == null)
                .Sum(x => x.Amount);
            return paidAmount == 0m ? null : $"Payment placed {paidAmount}";
        }

        private static List<string> DescribeNotifications(CreditContextExtended context, DateTime date, string creditNr, PaymentOrderService paymentOrderService)
        {
            var paymentOrder = paymentOrderService.GetPaymentOrderItems();
            var notificationIds = context.CreditNotificationHeaders.Where(x => x.TransactionDate == date && x.CreditNr == creditNr).Select(x => x.Id).ToList();
            return CreditNotificationDomainModel.CreateForNotifications(notificationIds, context, paymentOrder)
                .Select(x => x.Value)
                .Select(n =>
                {
                    var nDetails = string.Join(" ", paymentOrder.Select(x => new { Type = x.Code, Balance = n.GetRemainingBalance(date, x) }).Where(x => x.Balance > 0m).Select(x => $"{x.Type}={x.Balance}"));
                    return $"Notification created for {n.GetRemainingBalance(date)} ({nDetails})";
                })
                .ToList();
        }

        private static List<string> DescribeReminders(CreditContextExtended context, DateTime date, string creditNr)
        {
            return context
                .CreditReminderHeaders
                .Where(x => x.TransactionDate == date && x.CreditNr == creditNr)
                .Select(x => new
                {
                    NotificationDueDate = x.Notification.DueDate,
                    x.ReminderNumber,
                    Fee = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.ReminderFeeDebt.ToString() && y.TransactionDate == date && y.IncomingPaymentId == null).Sum(y => y.Amount)
                })
                .ToList()
                .OrderBy(x => x.NotificationDueDate)
                .ThenBy(x => x.ReminderNumber)
                .Select(x => $"Reminder {x.ReminderNumber} created for notification due {x.NotificationDueDate:yyyy-MM-dd} with Fee={x.Fee}")
                .ToList();
        }

        private static List<string> DescribeTerminationLetters(CreditContextExtended context, DateTime date, string creditNr)
        {
            return context
                .CreditTerminationLetterHeaders
                .Where(x => x.CreditNr == creditNr && x.TransactionDate == date)
                .Select(x => new { x.DueDate })
                .ToList()
                .Select(x => $"Termination letter created due {x.DueDate}")
                .ToList();
        }

        private static List<string> DescribeDebtCollection(CreditContextExtended context, DateTime date, string creditNr)
        {
            return context
                .DatedCreditStrings
                .Where(x => x.CreditNr == creditNr && x.TransactionDate == date && x.Name == DatedCreditStringCode.CreditStatus.ToString() && x.Value == CreditStatus.SentToDebtCollection.ToString())
                .ToList()
                .Select(x => $"Sent to debt collection")
                .ToList();
        }
    }
}