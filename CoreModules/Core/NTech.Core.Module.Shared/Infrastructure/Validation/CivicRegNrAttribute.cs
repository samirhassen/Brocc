using System;

namespace NTech.Services.Infrastructure.NTechWs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CivicRegNrAttribute : NTechWsStringValidationAttributeBase
    {
        private static Func<string, bool> isValidCivicRegNr = null;
        protected override bool IsValidString(string value)
        {
            if (isValidCivicRegNr == null)
                throw new Exception("Initialize has not beend called");
            return isValidCivicRegNr(value);
        }

        public static void Initialize(Func<string, bool> isValid)
        {
            isValidCivicRegNr = isValid;
        }
    }
}
