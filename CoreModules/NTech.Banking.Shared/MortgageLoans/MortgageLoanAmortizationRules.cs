using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.MortgageLoans
{
    public static class MortgageLoanAmortizationRules
    {

        /// <summary>
        /// Calculate required amortization using the 2016:16 framework:
        /// loan fraction > 50% => +1%
        /// loan fraction > 70% => +1%
        /// </summary>
        /// <param name="loanAmount">Loan amount used to compute loan fraction and by default also debt multiplier</param>
        /// <param name="objectValueAmount">Object value used for loan fraction, debt multiplier and amortization amount</param>
        /// <param name="debtMultiplierLoanAmount">Used instead of loanAmount for debt multiplier when included. For instance when moving a loan this will be the current loan balance while loan amount will be the amortizaton basis amount which will be older.</param>
        /// <param name="observeLoanFraction">Observe interim values</param>
        /// <param name="observeAmortizationPercent">Observe interim values</param>
        /// <returns>Rounded monthly amortization amount</returns>
        public static decimal CalcluateMimimumAmortizationUsingR201616(decimal loanAmount, decimal objectValueAmount, Action<decimal> observeLoanFraction = null, Action<decimal> observeAmortizationPercent = null)
        {
            if (objectValueAmount <= 0m)
                throw new Exception("Object value must be > 0");

            var loanFraction = Math.Round(loanAmount / objectValueAmount, 3);

            observeLoanFraction?.Invoke(loanFraction);

            var amortizationPercent = 0m;
            if (loanFraction > 0.5m)
                amortizationPercent += 1m;
            if (loanFraction > 0.7m)
                amortizationPercent += 1m;

            observeAmortizationPercent?.Invoke(amortizationPercent);

            return Math.Round((amortizationPercent / 100m) * loanAmount / 12m);
        }

        /// <summary>
        /// Calculate required amortization using the 2017:23 framework which is 2016:16 with the added:
        /// debt multiplier > 4,5 => +1%
        /// </summary>
        /// <param name="loanAmount">Loan amount used to compute loan fraction and by default also debt multiplier</param>
        /// <param name="otherMortgageLoansAmount">Included in the debt multiplier</param>
        /// <param name="objectValueAmount">Object value used for loan fraction, debt multiplier and amortization amount</param>
        /// <param name="combinedGrossMonthlyIncomeAmount">Income used for debt multiplier</param>
        /// <param name="debtMultiplierLoanAmount">Used instead of loanAmount for debt multiplier when included. For instance when moving a loan this will be the current loan balance while loan amount will be the amortizaton basis amount which will be older.</param>
        /// <param name="observeLoanFractionAndDebtMultiplier">Observe interim values</param>
        /// <param name="observeAmortizationPercent">Observe interim values</param>
        /// <returns>Rounded monthly amortization amount</returns>
        public static decimal CalcluateMimimumAmortizationUsingR201723(decimal loanAmount, decimal otherMortgageLoansAmount, decimal objectValueAmount, decimal combinedGrossMonthlyIncomeAmount, decimal? debtMultiplierLoanAmount = null, Action<Tuple<decimal, decimal?>> observeLoanFractionAndDebtMultiplier = null, Action<decimal> observeAmortizationPercent = null)
        {
            decimal? loanFraction = null;
            decimal? amortizationPercent = null;
            CalcluateMimimumAmortizationUsingR201616(loanAmount, objectValueAmount, observeLoanFraction: x => loanFraction = x, observeAmortizationPercent: x => amortizationPercent = x);

            decimal? debtMultiplier = null;
            var totalLoanAmount = (debtMultiplierLoanAmount ?? loanAmount) + otherMortgageLoansAmount;

            if (combinedGrossMonthlyIncomeAmount > 0)
            {
                debtMultiplier = Math.Round(totalLoanAmount / (combinedGrossMonthlyIncomeAmount * 12m), 2);
            }

            observeLoanFractionAndDebtMultiplier?.Invoke(Tuple.Create(loanFraction.Value, debtMultiplier));

            if (!debtMultiplier.HasValue || debtMultiplier.Value > 4.5m)
                amortizationPercent += 1m;

            observeAmortizationPercent?.Invoke(amortizationPercent.Value);

            return Math.Round((amortizationPercent.Value / 100m) * loanAmount / 12m);
        }
    }
}
