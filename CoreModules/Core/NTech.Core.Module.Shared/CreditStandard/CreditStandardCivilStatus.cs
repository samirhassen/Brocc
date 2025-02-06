using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public static class CreditStandardCivilStatus
    {
        public enum Code
        {
            single,
            co_habitant,
            married,
            divorced,
            widowed
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        public static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.single, "Single" },
            { Code.co_habitant, "Co habitant" },
            { Code.married, "Married" },
            { Code.divorced, "Divorced" },
            { Code.widowed, "Widowed" }
        };
    }
}
