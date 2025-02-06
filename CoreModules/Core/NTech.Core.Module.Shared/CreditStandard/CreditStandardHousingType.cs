using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public static class CreditStandardHousingType
    {
        public enum Code
        {
            condominium,
            house,
            rental,
            tenant
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        private static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.condominium, "Condominium" },
            { Code.house, "House" },
            { Code.rental, "Rental" },
            { Code.tenant, "Tenant" }
        };

        private static readonly Dictionary<Code, string> SwedishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.condominium, "Bostadsrätt/Ägarlägenhet" },
            { Code.house, "Hus" },
            { Code.rental, "Hyresrätt" },
            { Code.tenant, "Inneboende" }
        };

        public static string GetDisplayName(string code, string langauge)
        {
            if (Enum.TryParse<Code>(code, out var parsedCode))
            {
                if (langauge == "sv")
                    return SwedishDisplayNameByCode[parsedCode];
                else
                    return EnglishDisplayNameByCode[parsedCode];
            }
            else
                return code;
        }

        public static Dictionary<Code, string> GetDisplayNameCodes(string langauge) =>
            langauge == "sv" ? SwedishDisplayNameByCode : EnglishDisplayNameByCode;
    }
}
