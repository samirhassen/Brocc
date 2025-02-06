using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public static class CreditStandardEmployment
    {
        public enum Code
        {
            early_retiree,
            project_employee,
            full_time,
            hourly_employment,
            part_time,
            student,
            pensioner,
            unemployed,
            probationary,
            self_employed,
            substitute
        }

        public static List<Code> Codes => Enum.GetValues(typeof(Code)).Cast<Code>().ToList();

        public static readonly Dictionary<Code, string> EnglishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.early_retiree, "Early retiree" },
            { Code.project_employee, "Project employee" },
            { Code.full_time, "Full time" },
            { Code.hourly_employment, "Hourly employment" },
            { Code.part_time, "Part time" },
            { Code.student, "Student" },
            { Code.pensioner, "Pensioner" },
            { Code.unemployed, "Unemployed" },
            { Code.probationary, "Probationary" },
            { Code.self_employed, "Self-employed" },
            { Code.substitute, "Substitute" }
        };

        public static readonly Dictionary<Code, string> SwedishDisplayNameByCode = new Dictionary<Code, string>
        {
            { Code.early_retiree, "Förtidspensionär" },
            { Code.project_employee, "Projektanställd" },
            { Code.full_time, "Heltidsanställd" },
            { Code.hourly_employment, "Timanställd" },
            { Code.part_time, "Deltidsanställd" },
            { Code.student, "Student" },
            { Code.pensioner, "Pensionär" },
            { Code.unemployed, "Arbetslös" },
            { Code.probationary, "Provanställd" },
            { Code.self_employed, "Egenföretagare" },
            { Code.substitute, "Vikariat" }
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
