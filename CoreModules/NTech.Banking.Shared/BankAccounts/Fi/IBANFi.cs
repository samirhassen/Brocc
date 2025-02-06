using System;
using NTech.Banking.Shared.BankAccounts;

namespace NTech.Banking.BankAccounts.Fi
{
    public class IBANFi : IBAN, IBankAccountNumber
    {

        public new string TwoLetterCountryIsoCode => "FI";
        public new BankAccountNumberTypeCode AccountType => BankAccountNumberTypeCode.IBANFi;

        private IBANFi(string iban) : base(iban) { }

        public new static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }

        public new static IBANFi Parse(string iban)
        {
            IBANFi parsedValue;
            if(!TryParse(iban, out parsedValue))
            {
                throw new Exception("Invalid iban");
            }
            else
            {
                return parsedValue;
            }
        }

        public static bool TryParse(string iban, out IBANFi parsedValue)
        {
            parsedValue = null;

            iban = TrimIban(iban);

            // Must be of valid international IBAN rules as a minimum. 
            if (!TryParse(iban, out IBAN _))
                return false;

            //Check that the total IBAN length is correct as per the country. If not, the IBAN is invalid
            if (!iban.StartsWith("FI"))
                return false;

            // Pure characters for FI is always exactly 18 in length. 
            if (iban.Length != 18)
                return false;
            
            parsedValue = new IBANFi(iban);

            return true;
        }
    }
}