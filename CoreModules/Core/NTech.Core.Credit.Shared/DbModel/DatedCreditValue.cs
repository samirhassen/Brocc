using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public enum DatedCreditValueCode
    {
        MarginInterestRate,
        ReferenceInterestRate,
        AnnuityAmount,
        NotificationFee,
        MonthlyAmortizationAmount,
        /// <summary>
        /// Amortization used instead of ActualAmortizationAmount during the time until exception until date
        /// </summary>
        ExceptionAmortizationAmount,
        /// <summary>
        /// When the current margin interest rate is constrained by legal limits this stores what we want it to actually be.
        /// This will be used to move it back up when reference interest rate changes.
        /// </summary>
        RequestedMarginInterestRate,
        /// <summary>
        /// Something like 28 meaning the 28th of each month.
        /// </summary>
        NotificationDueDay,
        /// <summary>
        /// Esitmated loss given default from the application process
        /// </summary>
        ApplicationLossGivenDefault,
        /// <summary>
        /// Estimated probability of default from the application procss
        /// </summary>
        ApplicationProbabilityOfDefault,
        InitialRepaymentTimeInMonths,
        InitialEffectiveInterestRatePercent,
        MortgageLoanInterestRebindMonthCount,
        SinglePaymentLoanRepaymentDays
    }

    //For things like annuity and base/margin interest rate that can change over time but where the historical values have impact
    public class DatedCreditValue : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditHeader Credit { get; set; }
        public string CreditNr { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public decimal Value { get; set; }
    }
}