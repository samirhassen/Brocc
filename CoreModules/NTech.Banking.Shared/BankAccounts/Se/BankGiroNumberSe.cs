using NTech.Banking.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.BankAccounts.Se
{
    public class BankGiroNumberSe : IBankAccountNumber
    {
        private readonly string nr;

        private BankGiroNumberSe(string nr)
        {
            this.nr = nr;
        }

        public static BankGiroNumberSe Parse(string value)
        {
            BankGiroNumberSe p;
            string msg;
            if (!TryParseWithErrorMessage(value, out p, out msg))
                throw new Exception(msg);
            return p;
        }

        public string NormalizedValue
        {
            get
            {
                return this.nr;
            }
        }

        public string DisplayFormattedValue
        {
            get
            {
                return Strings.MaskString("xxx-xxxx", this.nr);
            }
        }

        public string TwoLetterCountryIsoCode => "SE";

        public BankAccountNumberTypeCode AccountType => BankAccountNumberTypeCode.BankGiroSe;

        public static bool TryParse(string value, out BankGiroNumberSe bankGiroNumberSe)
        {
            string _;
            return TryParseWithErrorMessage(value, out bankGiroNumberSe, out _);
        }

        public static bool TryParseWithErrorMessage(string value, out BankGiroNumberSe bankGiroNumberSe, out string errorMessage)
        {
            if(value == null)
            {
                errorMessage = "empty";
                bankGiroNumberSe = null;
                return false;
            }
            var cleanedNr = new string(value.Where(Char.IsNumber).ToArray()).TrimStart('0');
            if(cleanedNr.Length < 7 || cleanedNr.Length > 8)
            {
                errorMessage = "Invalid length";
                bankGiroNumberSe = null;
                return false;
            }

            if (ComputeMod10CheckDigit(cleanedNr.Substring(0, cleanedNr.Length-1)).ToString() != cleanedNr.Substring(cleanedNr.Length - 1, 1))
            {
                errorMessage = "Invalid check digit";
                bankGiroNumberSe = null;
                return false;
            }

            errorMessage = null;
            bankGiroNumberSe = new BankGiroNumberSe(cleanedNr);
            return true;
        }

        private static int ComputeMod10CheckDigit(string input)
        {
            return (10 - (input
                .Reverse()
                .Select((x, i) => (int.Parse(new string(new[] { x })) * (i % 2 == 0 ? 2 : 1)))
                .Sum(x => (x % 10) + (x >= 10 ? 1 : 0)) % 10)) % 10;
        }

        public string FormatFor(string formatName)
        {
            if (formatName == null)
                return this.NormalizedValue;
            else if (formatName.Equals("display", StringComparison.OrdinalIgnoreCase))
                return this.DisplayFormattedValue;
            else if (formatName.Equals("pain.001.001.3", StringComparison.OrdinalIgnoreCase))
                return this.NormalizedValue;
            else
                throw new Exception($"Unsupported format name: '{formatName}'");
        }
    }
}
