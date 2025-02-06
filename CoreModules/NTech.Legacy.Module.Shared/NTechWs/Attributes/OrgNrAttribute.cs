using System;

namespace NTech.Services.Infrastructure.NTechWs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OrgNrAttribute : NTechWsStringValidationAttributeBase
    {
        private static Func<string, bool> isValidOrgNr = null;
        protected override bool IsValidString(string value)
        {
            if (isValidOrgNr == null)
                throw new Exception("Initialize has not beend called");
            return isValidOrgNr(value);
        }

        public static void Initialize(Func<string, bool> isValid)
        {
            isValidOrgNr = isValid;
        }
    }
}
