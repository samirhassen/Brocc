using System;

namespace NTech.Services.Infrastructure.NTechWs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class BankAccountNrAttribute : NTechWsStringValidationAttributeBase
    {
        private static Func<string, bool> isValidBankAccountNr = null;
        protected override bool IsValidString(string value)
        {
            if (isValidBankAccountNr == null)
                throw new Exception("Initialize has not beend called");
            return isValidBankAccountNr(value);
        }

        public static void Initialize(Func<string, bool> isValid)
        {
            isValidBankAccountNr = isValid;
        }
    }
}
