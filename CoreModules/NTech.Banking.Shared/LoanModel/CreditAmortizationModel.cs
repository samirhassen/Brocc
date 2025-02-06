using System;

namespace NTech.Banking.LoanModel
{
    public class CreditAmortizationModel
    {
        private decimal? annuityAmount;
        private decimal? monthlyFixedCapitalAmount;
        private readonly DateTime? amortizationFreeUntilDate;
        private readonly DateTime? amortizationExceptionUntilDate;
        private readonly decimal? exceptionAmortizationAmount;

        public static CreditAmortizationModel CreateAnnuity(decimal annuityAmount, DateTime? amortizationFreeUntilDate)
        {
            return new CreditAmortizationModel(annuityAmount, null, amortizationFreeUntilDate, null, null);
        }

        public static CreditAmortizationModel CreateMonthlyFixedCapitalAmount(decimal monthlyFixedCapitalAmount, DateTime? amortizationFreeUntilDate, DateTime? amortizationExceptionUntilDate, decimal? exceptionAmortizationAmount)
        {
            return new CreditAmortizationModel(null, monthlyFixedCapitalAmount, amortizationFreeUntilDate, amortizationExceptionUntilDate, exceptionAmortizationAmount);
        }

        private CreditAmortizationModel(decimal? annuityAmount, decimal? monthlyFixedCapitalAmount, DateTime? amortizationFreeUntilDate, DateTime? amortizationExceptionUntilDate, decimal? exceptionAmortizationAmount)
        {
            if (annuityAmount.HasValue && monthlyFixedCapitalAmount.HasValue)
                throw new Exception("Cannot have both annuityAmount and monthlyFixedCapitaAmount");
            if (!annuityAmount.HasValue && !monthlyFixedCapitalAmount.HasValue)
                throw new Exception("Permanently amortization free is not supported");
            this.annuityAmount = annuityAmount;
            this.monthlyFixedCapitalAmount = monthlyFixedCapitalAmount;
            this.amortizationExceptionUntilDate = amortizationExceptionUntilDate;
            this.amortizationFreeUntilDate = amortizationFreeUntilDate;
            this.exceptionAmortizationAmount = exceptionAmortizationAmount;
        }

        public DateTime? AmortizationFreeUntilDate { get { return this.amortizationFreeUntilDate; } }
        public DateTime? AmortizationExceptionUntilDate { get { return this.amortizationExceptionUntilDate; } }

        public bool UsesAnnuities
        {
            get
            {
                return annuityAmount.HasValue;
            }
        }

        public bool IsAmortizationFree(DateTime transactionDate)
        {
            return amortizationFreeUntilDate.HasValue && amortizationFreeUntilDate.Value.Date >= transactionDate.Date;
        }

        public bool HasAmortizationException(DateTime transactionDate)
        {
            return amortizationExceptionUntilDate.HasValue && amortizationExceptionUntilDate.Value.Date >= transactionDate.Date;
        }

        public decimal? GetAmortizationOverrideAmount(DateTime transactionDate)
        {
            if (IsAmortizationFree(transactionDate))
                return 0m;
            else if (HasAmortizationException(transactionDate))
                return exceptionAmortizationAmount;
            else
                return null;
        }

        /// <summary>
        /// Amortization amount used when there are no overrides.
        /// </summary>
        /// <returns></returns>
        public TResult UsingActualAnnuityOrFixedMonthlyCapital<TResult>(Func<decimal, TResult> onAnnuity, Func<decimal, TResult> onFixedMonthlyPayment)
        {
            if (annuityAmount.HasValue)
                return onAnnuity(annuityAmount.Value);
            else
                return onFixedMonthlyPayment(monthlyFixedCapitalAmount.Value);
        }

        /// <summary>
        /// Amortization amount used currently taking any overrides into account.
        /// Note that when an override is active, even annuity loans are in practice following a fixed monthly payment plan.
        /// </summary>
        /// <returns></returns>
        public TResult UsingCurrentAnnuityOrFixedMonthlyCapital<TResult>(DateTime transactionDate, Func<decimal, TResult> onAnnuity, Func<decimal, TResult> onFixedMonthlyPayment)
        {
            var overrideAmount = GetAmortizationOverrideAmount(transactionDate);
            if (overrideAmount.HasValue)
                return onFixedMonthlyPayment(overrideAmount.Value);

            return UsingActualAnnuityOrFixedMonthlyCapital(onAnnuity, onFixedMonthlyPayment);
        }

        /// <summary>
        /// Fixed amortization amount used when there is no exception or amortization freedom. Throws if the payment model is not fixed monthly amortization.
        /// </summary>
        public decimal GetActualFixedMonthlyPaymentOrException()
        {
            return UsingActualAnnuityOrFixedMonthlyCapital(_ => throw new Exception("Loan uses annuities"), x => x);
        }

        /// <summary>
        /// Fixed amortization amount used taking into account any overrides.
        /// </summary>
        /// <returns></returns>
        public decimal GetCurrentFixedMonthlyPaymentOrException(DateTime transactionDate)
        {
            var actualFixedMonthlyPayment = GetActualFixedMonthlyPaymentOrException();
            var overrideAmount = GetAmortizationOverrideAmount(transactionDate);
            return overrideAmount ?? actualFixedMonthlyPayment;
        }

        /// <summary>
        /// Annuity amount used when there is no exception or amortization freedom. Throws if the payment model is not annuities.
        /// </summary>
        public decimal GetActualAnnuityOrException()
        {
            return UsingActualAnnuityOrFixedMonthlyCapital(x => x, _ => throw new Exception("Loan uses fixed monthly payments"));
        }

        public decimal GetNotificationCapitalAmount(DateTime notificationDate, DateTime dueDate, decimal notificationInterestAmount)
        {
            var overrideAmount = GetAmortizationOverrideAmount(dueDate);
            if (overrideAmount.HasValue)
                return overrideAmount.Value;

            return UsingCurrentAnnuityOrFixedMonthlyCapital(
                dueDate,
                annuityAmount => annuityAmount - notificationInterestAmount,
                fixedAmount => fixedAmount);
        }

        //remainingCapitalAmount rather than balance since we are simluating what happens after a new notification is created and that captital is not included in balance
        public bool ShouldCarryOverRemainingCapitalAmount(DateTime notificationDate, DateTime dueDate, decimal remainingCapitalAmount, PaymentPlanCalculation.Settings settings)
        {
            return UsingCurrentAnnuityOrFixedMonthlyCapital(
                dueDate,
                annuityAmount => settings.ShouldCarryOverRemainingCapitalAmount(remainingCapitalAmount, annuityAmount, null),
                fixedAmount => settings.ShouldCarryOverRemainingCapitalAmount(remainingCapitalAmount, null, fixedAmount));
        }
    }
}