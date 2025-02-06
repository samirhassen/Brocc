using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.MortgageLoans
{
    public enum MortageLoanAmortizationRuleCode
    {
        /// <summary>
        /// No amortization required
        /// </summary>
        none,

        /// <summary>
        /// > 50% loan fraction: 1%, > 70% loan fraction: an additional 1%
        /// </summary>
        r201616,

        /// <summary>
        /// r201616 + an additional 1% if loan income ratio > 4.5
        /// </summary>
        r201723,

        /// <summary>
        /// Fixed amount (the actual rule is that the loan is to be paid over 10 years but the original balance will typically not be known so from a system perspective this is just a fixed monthly amount)
        /// </summary>
        alternate
    }
}
