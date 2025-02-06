using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public static class CreditStandardOtherLoanType
    {
        public enum Code
        {
            unknown,
            student,
            mortgage,
            personal,
            car,
            creditcard,
            boat,
            other
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        public static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.unknown, "Unknown" },
            { Code.student, "Student loan" },
            { Code.mortgage, "Mortgage loan" },
            { Code.personal, "Personal loan" }, //Privatlån
            { Code.car, "Car loan" },
            { Code.creditcard, "Credit card balance" },
            { Code.boat, "Boat loan" },
            { Code.other, "Other" }
        };

        public static readonly Dictionary<Code, string> SwedishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.unknown, "Okänd" },
            { Code.student, "Studielån" },
            { Code.mortgage, "Bolån" },
            { Code.personal, "Personligt lån" }, //Privatlån
            { Code.car, "Billån" },
            { Code.creditcard, "Kreditkortsskuld" },
            { Code.boat, "Båtlån" },
            { Code.other, "Annat" }
        };

        public static Dictionary<Code, string> GetDisplayNameCodes(string langauge)
        {
            if (langauge == "sv")
                return SwedishDisplayNameByCode;
            else if (langauge == "en")
                return EnglishDisplayNameByCode;
            else
                return EnglishDisplayNameByCode;
        }
    }
}
