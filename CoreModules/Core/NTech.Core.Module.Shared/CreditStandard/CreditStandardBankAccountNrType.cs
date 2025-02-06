using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public static class CreditStandardBankAccountNrType
    {
        public enum Code
        {
            regular,
            bankGiro,
            plusGiro
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        public static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.regular, "Regular account" },
            { Code.bankGiro, "Plusgiro" },
            { Code.plusGiro, "Bankgiro" }
        };
    }
}
