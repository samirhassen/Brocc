using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace nUser
{
    public class FinnishCivicRegNumber
    {
        public static bool IsValidFinnishCivicRegNr(string value, out string normalizedCivicRegNr)
        {
            //http://apps.vurdalakov.net/hetu/
            normalizedCivicRegNr = (value ?? "").ToUpper();

            if (normalizedCivicRegNr.Length != 11)
                return false;

            String separator = normalizedCivicRegNr.Substring(6, 1);
            if (!((separator.Equals("-")) || (separator.Equals("A")) || (separator.Equals("+"))))
                return false;

            Regex sixNumbers = new Regex(@"(?<!\d)\d{6}(?!\d)");
            if (!sixNumbers.IsMatch(normalizedCivicRegNr.Substring(0, 6)))
            {
                return false;
            }

            Regex threeNumbers = new Regex(@"(?<!\d)\d{3}(?!\d)");
            if (!threeNumbers.IsMatch(normalizedCivicRegNr.Substring(7, 3)))
            {
                return false;
            }


            Func<string, string> generateChecksum = userCivicRegNr =>
            {
                int index = Int32.Parse(userCivicRegNr.Substring(0, 6) + userCivicRegNr.Substring(7, 3)) % 31;
                return "0123456789ABCDEFHJKLMNPRSTUVWXY".Substring(index, 1);
            };

            string checksum1 = normalizedCivicRegNr.Substring(10, 1);
            string checksum2 = generateChecksum(normalizedCivicRegNr);
            if (!checksum1.Equals(checksum2))
            {
                return false;
            }

            string date = normalizedCivicRegNr.Substring(0, 6);
            DateTime dateValue;
            if (!DateTime.TryParseExact(date, "ddMMyy", null, DateTimeStyles.None, out dateValue))
            {
                return false;
            }

            return true;
        }
    }
}