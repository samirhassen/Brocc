using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NTech.Banking.BankAccounts;

namespace NTech.Banking.Shared.BankAccounts
{

    public interface IIBAN : IBankAccountNumber
    {

    }

    /// <summary>
    /// This class corresponds to all internationa IBANs.
    /// https://www.iban.com/structure 
    /// </summary>
    public class IBAN : IIBAN
    {

        protected readonly string iban;

        protected IBAN(string iban)
        {
            this.iban = iban;
        }

        public string TwoLetterCountryIsoCode => iban.Substring(0, 2);
        public BankAccountNumberTypeCode AccountType => BankAccountNumberTypeCode.IBAN;

        public virtual string FormatFor(string formatName)
        {
            if (formatName == null)
                return NormalizedValue;
            if (formatName.Equals("display", StringComparison.OrdinalIgnoreCase))
                return GroupsOfFourValue;
            else
                throw new NotImplementedException();
        }
        
        protected static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        public string GroupsOfFourValue
        {
            get
            {
                //Group into groups of 4 from the left (123456789 -> 1234 5678 9)
                return string.Join(" ",
                    SplitIntoGroupsOfN(iban.ToArray(), 4)
                        .Select(x => new string(x.ToArray())));
            }
        }
        
        public string NormalizedValue => this.iban;

        public static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }

        public static IBAN Parse(string iban)
        {
            if (!TryParse(iban, out var parsedValue))
            {
                throw new Exception("Invalid iban");
            }
            else
            {
                return parsedValue;
            }
        }

        /// <summary>
        /// Details: https://en.wikipedia.org/wiki/International_Bank_Account_Number
        /// </summary>
        /// <param name="iban">The IBAN in string format. </param>
        /// <param name="parsedValue"></param>
        /// <returns>Bool if valid IBAN or not. </returns>
        public static bool TryParse(string iban, out IBAN parsedValue)
        {
            parsedValue = null;

            if (string.IsNullOrWhiteSpace(iban))
                return false;

            iban = TrimIban(iban);

            if (iban.Length < 2)
                return false;

            // Must start with two letters. 
            if (!iban.Substring(0, 2).All(char.IsLetter))
                return false;

            // Norway shortest with 15, max length is 34. 
            if (iban.Length < 15 || iban.Length > 34)
                return false;

            if (!HasValidIbanCheckDigit(iban))
                return false;

            parsedValue = new IBAN(iban);

            return true;
        }

        /// <summary>
        /// All international IBAN uses Mod97.
        /// https://en.wikipedia.org/wiki/International_Bank_Account_Number#Processing
        /// </summary>
        /// <param name="iban"></param>
        /// <returns></returns>
        private static bool HasValidIbanCheckDigit(string iban)
        {
            //Move the four initial characters to the end of the string
            var s = iban.Substring(4) + iban.Substring(0, 4);

            //Replace each letter in the string with two digits, thereby expanding the string, where A = 10, B = 11, ..., Z = 35
            var delta = Convert.ToInt32('A');
            s = string.Join("", s.Select(x => Char.IsLetter(x) ? (Convert.ToInt32(x) - delta + 10).ToString() : x.ToString()));
            //Interpret the string as a decimal integer and compute the remainder of that number on division by 97
            var rem = BigModulo(s, 97);

            //If the remainder is 1, the check digit test is passed and the IBAN might be valid.
            return (rem == 1);
        }

        private static int BigModulo(string nr, int divisor)
        {
            //Source: http://www.devx.com/tips/Tip/39012
            const int PartLength = 9;
            while (nr.Length > PartLength)
            {
                var part = nr.Substring(0, PartLength);
                var mod = int.Parse(part) % divisor;
                nr = string.Format("{0}{1}", mod, nr.Substring(PartLength));
            }
            return int.Parse(nr) % divisor;
        }

        protected static string TrimIban(string iban)
        {
            return iban.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        }

    }
}
