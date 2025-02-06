using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nGccCustomerApplication.Code
{
    public class CivicRegNumberFi
    {
        private readonly string normalizedValue;

        public string NormalizedValue
        {
            get
            {
                return this.normalizedValue;
            }
        }

        public DateTime BirthDate
        {
            get
            {
                return ParseBirthDate(this.normalizedValue).Value;
            }
        }

        private CivicRegNumberFi(string normalizedValue)
        {
            this.normalizedValue = normalizedValue;
        }

        public static bool IsValid(string value)
        {
            CivicRegNumberFi _;
            return TryParse(value, out _);
        }

        private static DateTime? ParseBirthDate(string civicRegNr)
        {
            string ddmm = civicRegNr.Substring(0, 4);
            string yy = civicRegNr.Substring(4, 2);

            Func<string, string> c = s =>
            {
                switch (s[6])
                {
                    case '+': return "18";
                    case '-': return "19";
                    case 'A': return "20";
                    default: throw new NotImplementedException();
                }
            };

            DateTime dateValue;
            if (!DateTime.TryParseExact(ddmm + c(civicRegNr) + yy, "ddMMyyyy", null, DateTimeStyles.None, out dateValue))
                return null;
            return dateValue;
        }

        public static CivicRegNumberFi Parse(string value)
        {
            CivicRegNumberFi c;
            if (!CivicRegNumberFi.TryParse(value, out c))
                throw new ArgumentException("Invalid civicRegNr", "value");
            return c;
        }

        public static bool TryParse(string value, out CivicRegNumberFi civicRegNr)
        {
            //Generate testvalues: http://apps.vurdalakov.net/hetu/
            civicRegNr = null;

            var normalizedCivicRegNr = (value ?? "").ToUpper();


            if (normalizedCivicRegNr.Length != 11)
                return false;

            String separator = normalizedCivicRegNr.Substring(6, 1);
            if (!((separator.Equals("-")) || (separator.Equals("A")) || (separator.Equals("+"))))
                return false;

            Regex sixNumbers = new Regex(@"(?<!\d)\d{6}(?!\d)");
            if (!sixNumbers.IsMatch(normalizedCivicRegNr.Substring(0, 6)))
                return false;

            Regex threeNumbers = new Regex(@"(?<!\d)\d{3}(?!\d)");
            if (!threeNumbers.IsMatch(normalizedCivicRegNr.Substring(7, 3)))
                return false;
            
            Func<string, string> generateChecksum = userCivicRegNr =>
            {
                int index = Int32.Parse(userCivicRegNr.Substring(0, 6) + userCivicRegNr.Substring(7, 3)) % 31;
                return "0123456789ABCDEFHJKLMNPRSTUVWXY".Substring(index, 1);
            };

            string checksum1 = normalizedCivicRegNr.Substring(10, 1);
            string checksum2 = generateChecksum(normalizedCivicRegNr);
            if (!checksum1.Equals(checksum2))
                return false;

            var date = ParseBirthDate(normalizedCivicRegNr);
            if (date == null)
                return false;

            civicRegNr = new CivicRegNumberFi(normalizedCivicRegNr);
            return true;
        }
    }
}
