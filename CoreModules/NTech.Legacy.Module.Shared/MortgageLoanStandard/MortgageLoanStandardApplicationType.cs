using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.MortgageLoanStandard
{
    public static class MortgageLoanStandardApplicationType
    {
        public enum Code
        {
            newLoan,
            moveExistingLoan,
            additionalLoan
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        public static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.newLoan, "New Loan" },
            { Code.moveExistingLoan, "Move loan" },
            { Code.additionalLoan, "Additional loan" }
        };
    }
}
