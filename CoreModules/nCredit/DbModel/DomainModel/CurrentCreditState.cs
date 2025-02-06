using NTech;
using NTech.Banking.LoanModel;
using System;
using System.Linq;

namespace nCredit.DbModel.DomainModel
{
    public class CurrentCreditState
    {
        private static RemainingPaymentsCalculation remainingCalculation = new RemainingPaymentsCalculation();

        public string CreditNr { get; set; }
        public int CustomerId { get; set; }
        public byte[] Timestamp { get; set; }
        public DateTime CreationDate { get; set; }
        public decimal? InitalCapitalDebt { get; set; }
        public decimal? TotalNotifiedInterest { get; set; }
        public decimal? TotalPaidInterest { get; set; }
        public decimal? TotalNotifiedFees { get; set; }
        public decimal? TotalPaidNotifiedFees { get; set; }
        public decimal? TotalPaidInitialFees { get; set; }
        public decimal? TotalNotifiedCapital { get; set; }
        public decimal? MarginInterestRate { get; set; }
        public decimal? ReferenceInterestRate { get; set; }
        public decimal? AnnuityAmount { get; set; }
        public decimal? CapitalDebt { get; set; }
        public string ProviderName { get; set; }
        public string CompanyLoanSniKodSe { get; set; }
        public decimal? CreationCapitalBalance { get; set; }
        public decimal? CreationMarginInterestRate { get; set; }
        public decimal? CreationReferenceInterestRate { get; set; }
        public decimal? CreationAnnuityAmount { get; set; }
        public decimal? CurrentNotNotifiedCapitalBalance { get; set; }
        public DateTime? LatestNotificationDueDate { get; set; }
        public int? LatestNotificationId { get; set; }
        public int NrOfClosedNotifications { get; set; }
        public string NextInterestFromDate { get; set; }
        public decimal? CurrentNotificationFeeAmount { get; set; }
        public string Status { get; set; }
        public DateTime? StatusDate { get; set; }
        public decimal? ApplicationProbabilityOfDefault { get; set; }
        public decimal? ApplicationLossGivenDefault { get; set; }

        public DateTime? GetParsedNextInterestFromDate() =>
            NextInterestFromDate == null ? null : Dates.ParseDateTimeExactOrNull(NextInterestFromDate, "yyyy-MM-dd");

        public decimal GetCurrentInterestRate() =>
            MarginInterestRate.GetValueOrDefault() + ReferenceInterestRate.GetValueOrDefault();

        public decimal GetInitialInterestRate() =>
            CreationMarginInterestRate.GetValueOrDefault() + CreationReferenceInterestRate.GetValueOrDefault();

        /// <summary>
        /// Tries to handle the fact that a new credit that is created before the notifications for month have run vs after
        /// lead to a one month shift.
        /// 
        /// We would prefer this to not crash and burn if the notification job is triggered one day late because of a holiday or downtime.
        /// 
        /// Here we use the heuristic that if we have passed the notification date by more than a week they will never run.
        /// Note that this has no effect on credits that have been notified at least once as the model then just keeps going from the last due date and "now" doesnt matter.

        /// </summary>
        DateTime GetNextAllowedFixedDueDate(DateTime forDate, NotificationProcessSettings notificationSettings)
        {
            //BEWARE: This will not work when the client users per notification due dates but could likely be extended to support it by also sending the PerLoanDueDay as an int? here and taking that if not null and otherwise falling back to the shared one
            var expectedMonthlyDueDate = new DateTime(forDate.Year, forDate.Month, notificationSettings.NotificationDueDay);
            var latestAllowedMonthlyNotificationDate = new DateTime(forDate.Year, forDate.Month, notificationSettings.NotificationNotificationDay).AddDays(7);
            var nextAllowedDueDate = forDate <= latestAllowedMonthlyNotificationDate ? expectedMonthlyDueDate : expectedMonthlyDueDate.AddMonths(1);

            return nextAllowedDueDate;
        }

        public RemainingPaymentsCalculation.RemainingPaymentsModel GetCurrentRemainingPayments(DateTime currentDate, NotificationProcessSettings notificationSettings) =>
            remainingCalculation.ComputeWithAnnuity(
                            LatestNotificationDueDate,
                            GetNextAllowedFixedDueDate(currentDate, notificationSettings),
                            CurrentNotNotifiedCapitalBalance.GetValueOrDefault(),
                            GetCurrentInterestRate(),
                            AnnuityAmount.GetValueOrDefault());

        public RemainingPaymentsCalculation.RemainingPaymentsModel GetInitialRemainingPayments(NotificationProcessSettings notificationSettings) =>
            remainingCalculation.ComputeWithAnnuity(
                            null,
                            GetNextAllowedFixedDueDate(CreationDate, notificationSettings),
                            CreationCapitalBalance.GetValueOrDefault(),
                            GetInitialInterestRate(),
                            this.CreationAnnuityAmount.GetValueOrDefault());


        public static IQueryable<CurrentCreditState> GetCreditsQueryable(CreditContext context, DateTime transactionDate)
        {
            var d = transactionDate.Date;
            return context
                .CreditHeaders.Where(x => x.CreatedByEvent.TransactionDate <= d)
                .Select(x => new CurrentCreditState
                {
                    CreditNr = x.CreditNr,
                    CustomerId = x.CreditCustomers.Select(y => y.CustomerId).FirstOrDefault(),
                    Timestamp = x.CreatedByEvent.Timestamp,
                    CreationDate = x.CreatedByEvent.TransactionDate,
                    InitalCapitalDebt = x
                            .CreatedByEvent
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                    TotalNotifiedInterest =
                         x
                            .Transactions
                            .Where(y => y.CreditNotificationId.HasValue && y.TransactionDate <= d && y.AccountCode == TransactionAccountType.InterestDebt.ToString() && y.Amount > 0)
                            .Sum(y => (decimal?)y.Amount),
                    TotalPaidInterest =
                         x
                            .Transactions
                            .Where(y => y.TransactionDate <= d && y.AccountCode == TransactionAccountType.InterestDebt.ToString() && y.Amount < 0)
                            .Sum(y => (decimal?)-y.Amount),
                    TotalNotifiedFees =
                         x
                            .Transactions
                            .Where(y => y.CreditNotificationId.HasValue && y.TransactionDate <= d && y.Amount > 0 && (y.AccountCode == TransactionAccountType.NotificationFeeDebt.ToString() || y.AccountCode == TransactionAccountType.ReminderFeeDebt.ToString()))
                            .Sum(y => (decimal?)y.Amount),
                    TotalPaidNotifiedFees =
                         x
                            .Transactions
                            .Where(y => y.CreditNotificationId.HasValue && y.TransactionDate <= d && y.Amount < 0 && (y.AccountCode == TransactionAccountType.NotificationFeeDebt.ToString() || y.AccountCode == TransactionAccountType.ReminderFeeDebt.ToString()))
                            .Sum(y => (decimal?)-y.Amount),
                    TotalPaidInitialFees =
                         x
                            .Transactions
                            .Where(y => y.TransactionDate <= d && (y.AccountCode == TransactionAccountType.InitialFeeDrawnFromLoanAmount.ToString()) && y.Amount > 0)
                            .Sum(y => (decimal?)y.Amount),
                    TotalNotifiedCapital =
                         x
                            .Transactions
                            .Where(y => y.CreditNotificationId.HasValue && y.TransactionDate <= d && y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                            .Sum(y => (decimal?)-y.Amount),
                    MarginInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    ReferenceInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    AnnuityAmount = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    CapitalDebt = x
                            .Transactions
                            .Where(y => y.TransactionDate <= d && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                    ProviderName = x.ProviderName,
                    Status = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                    StatusDate = (DateTime?)x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (DateTime?)y.TransactionDate)
                            .FirstOrDefault(),
                    CompanyLoanSniKodSe = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CompanyLoanSniKodSe.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                    CreationCapitalBalance = x
                            .CreatedByEvent
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                    CreationMarginInterestRate = x
                            .CreatedByEvent
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    CreationReferenceInterestRate = x
                            .CreatedByEvent
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    CreationAnnuityAmount = x
                            .CreatedByEvent
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    CurrentNotNotifiedCapitalBalance = x
                            .Transactions
                            .Where(y => y.TransactionDate <= d && y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                            .Sum(y => (decimal?)y.Amount),
                    LatestNotificationDueDate = x
                            .Notifications
                            .Where(y => y.TransactionDate <= d)
                            .OrderByDescending(y => y.DueDate)
                            .Select(y => (DateTime?)y.DueDate)
                            .FirstOrDefault(),
                    LatestNotificationId = x
                            .Notifications
                            .Where(y => y.TransactionDate <= d)
                            .OrderByDescending(y => y.DueDate)
                            .Select(y => (int?)y.Id)
                            .FirstOrDefault(),
                    NrOfClosedNotifications = x.Notifications.Count(y => y.ClosedTransactionDate.HasValue && y.ClosedTransactionDate.Value <= d),
                    NextInterestFromDate = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.NextInterestFromDate.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                    CurrentNotificationFeeAmount = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.NotificationFee.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    ApplicationProbabilityOfDefault = x //Intentionally always using the latest value
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.ApplicationProbabilityOfDefault.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    ApplicationLossGivenDefault = x //Intentionally always using the latest value
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.ApplicationLossGivenDefault.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                });
        }

    }
}