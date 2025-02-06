using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Models
{
    public class SwedishMortgageLoanAmortizationBasisStorageModel
    {
        /// <summary>
        /// Change this whenever the class SwedishMortgageLoanAmortizationBasisModel is changed in any way
        /// to allow special logic or migration of old ones. Always make the next version > the current.
        /// </summary>
        public const long CurrentVersion = 2022120801;

        public long Version { get; set; } = CurrentVersion;

        /// <summary>
        /// Date when this model was written to the system
        /// </summary>
        public DateTime TransactionDate { get; set; }

        public SwedishMortgageLoanAmortizationBasisModel Model { get; set; }

        public static SwedishMortgageLoanAmortizationBasisStorageModel Parse(string storedData)
        {
            return storedData == null ? null : JsonConvert.DeserializeObject<SwedishMortgageLoanAmortizationBasisStorageModel>(storedData);
        }
    }

    public class SwedishMortgageLoanAmortizationBasisModel
    {
        /// <summary>
        /// Date that the value is from
        /// </summary>
        [Required]
        public DateTime ObjectValueDate { get; set; }

        /// <summary>
        /// Värdering av objektet/valuation of the collateral
        /// </summary>
        [Required]
        [PositiveNumber]
        public decimal ObjectValue { get; set; }

        /// <summary>
        /// Loan to income/skuldkvot.
        /// Optional to include. Can also be calculated as:
        /// (OtherMortageLoansAmount + sum[Loans.CurrentCapitalBalanceAmount]) / CurrentCombinedYearlyIncomeAmount
        /// </summary>
        [NonNegativeNumber]
        public decimal? LtiFraction { get; set; }

        /// <summary>
        /// Loan to value/belåningsgrad
        /// Optional to include. Can also be calculated as:
        /// sum[Loans.CurrentCapitalBalanceAmount] / AmortizationBasisObjectValue
        /// </summary>
        [NonNegativeNumber]
        public decimal? LtvFraction { get; set; }

        /// <summary>
        /// Hushållets bruttoinkomst/Households yeary income
        /// Needed to [re]calculate the lti.
        /// </summary>
        [Required]
        [NonNegativeNumber]
        public decimal CurrentCombinedYearlyIncomeAmount { get; set; }

        /// <summary>
        /// Balans för hushållets bolån på andra säkerheter
        /// Balance of household mortgage loans on other collaterals
        /// Needed to [re]calculate the lti
        /// </summary>
        [Required]
        [NonNegativeNumber]
        public decimal OtherMortageLoansAmount { get; set; }

        public decimal GetTotalAmortizationBasisLoanAmount()
        {
            return Loans?.Where(x => x.CurrentCapitalBalanceAmount > 0m).Sum(x => x.MaxCapitalBalanceAmount ?? x.CurrentCapitalBalanceAmount) ?? 0m;
        }

        [Required]
        public List<LoanModel> Loans { get; set; }

        public class LoanModel
        {
            /// <summary>
            /// Lånenr/creditnr
            /// </summary>
            [Required]
            public string CreditNr { get; set; }

            /// <summary>
            /// Kapitalbalans/capital debt balance
            /// </summary>
            [Required]
            [NonNegativeNumber]
            public decimal CurrentCapitalBalanceAmount { get; set; }

            /// <summary>
            /// Max kapital/maxium capital debt. 
            /// Used in place of CurrentCapitalBalanceAmount if present when calculating required amortization.
            /// </summary>
            [PositiveNumber]
            public decimal? MaxCapitalBalanceAmount { get; set; }

            /// <summary>
            /// none, r201616 (Amorteringskrav), r201723 (Skärpt Amorteringskrav)
            /// If empty will be interpreted as the latest framework which currently is 2017:23
            /// </summary>
            [IsAnyOf(new[] { "none", "r201616", "r201723" })]
            public string RuleCode { get; set; }

            /// <summary>
            /// Följer alternativregeln
            /// </summary>
            public bool IsUsingAlternateRule { get; set; }

            /// <summary>
            /// Decided actual amortization amount
            /// The amount can initially be different according to MonthlyExceptionAmortizationAmount
            /// </summary>
            [Required]
            [NonNegativeNumber]
            public decimal MonthlyAmortizationAmount { get; set; }

            /// <summary>
            /// Interest bind month count
            /// </summary>
            public decimal InterestBindMonthCount { get; set; } //TODO: This is being removed
        }
    }
}
