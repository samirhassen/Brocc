using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public static class CreditStandardLoanPurpose
    {
        public enum Code
        {
            settleOtherLoans,
            newLoan
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        public static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.settleOtherLoans, "Settle loans" },
            { Code.newLoan, "New loan" }
        };
    }
}
