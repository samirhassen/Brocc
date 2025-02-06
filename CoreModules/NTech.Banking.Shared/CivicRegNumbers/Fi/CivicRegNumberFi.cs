using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NTech.Banking.CivicRegNumbers.Fi
{
    public class CivicRegNumberFi : CivicRegNumberBase, IEquatable<CivicRegNumberFi>, IComparable<CivicRegNumberFi>
    {
        public static HashSet<char> CenturyMarker18 = new HashSet<char> { '+' };
        public static HashSet<char> CenturyMarker19 = new HashSet<char> { '-', 'Y', 'X', 'W', 'V', 'U' };
        public static HashSet<char> CenturyMarker20 = new HashSet<char> { 'A', 'B', 'C', 'D', 'E', 'F' };

        private readonly string normalizedValue;

        public override string NormalizedValue
        {
            get
            {
                return this.normalizedValue;
            }
        }

        public override string Country
        {
            get
            {
                return "FI";
            }
        }

        public override DateTime? BirthDate
        {
            get
            {
                return ParseBirthDate(this.normalizedValue);
            }
        }

        public override bool? IsMale
        {
            get
            {
                return int.Parse(this.normalizedValue.Substring(7, 3)) % 2 != 0;
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

            /* Being reformed in 2024 to allow more infixes for 19<..> and 20<..>
             For those born in or after 2000, the current A or the new B, C, D, E, F.
             For those born in the 20th century, the current hyphen (-) or the new letters Y, X, W, V, U.
             For those born in the 19th century, the current plus sign (+) – no new intermediate characters will be introduced for such cases.         
             */
            Func<string, string> c = s =>
            {
                var centuryMarker = s[6];
                if (CenturyMarker18.Contains(centuryMarker))
                    return "18";
                else if (CenturyMarker19.Contains(centuryMarker))
                    return "19";
                else if (CenturyMarker20.Contains(centuryMarker))
                    return "20";
                else
                    throw new NotImplementedException();
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
        
        internal static string GenerateCheckDigit(string prefixOrCivicRegNr)
        {
            int index = Int32.Parse(prefixOrCivicRegNr.Substring(0, 6) + prefixOrCivicRegNr.Substring(7, 3)) % 31;
            return "0123456789ABCDEFHJKLMNPRSTUVWXY".Substring(index, 1);
        }

        public static bool TryParse(string value, out CivicRegNumberFi civicRegNr)
        {
            //Generate testvalues: http://apps.vurdalakov.net/hetu/
            civicRegNr = null;

            var normalizedCivicRegNr = (value ?? "").ToUpper().Trim();

            if (normalizedCivicRegNr.Length != 11)
                return false;

            char centuryMarker = normalizedCivicRegNr[6];
            if (!(CenturyMarker18.Contains(centuryMarker) || CenturyMarker19.Contains(centuryMarker) || CenturyMarker20.Contains(centuryMarker)))
                return false;

            Regex sixNumbers = new Regex(@"(?<!\d)\d{6}(?!\d)");
            if (!sixNumbers.IsMatch(normalizedCivicRegNr.Substring(0, 6)))
                return false;

            Regex threeNumbers = new Regex(@"(?<!\d)\d{3}(?!\d)");
            if (!threeNumbers.IsMatch(normalizedCivicRegNr.Substring(7, 3)))
                return false;

            string checksum1 = normalizedCivicRegNr.Substring(10, 1);
            string checksum2 = GenerateCheckDigit(normalizedCivicRegNr);
            if (!checksum1.Equals(checksum2))
                return false;

            var date = ParseBirthDate(normalizedCivicRegNr);
            if (date == null)
                return false;

            civicRegNr = new CivicRegNumberFi(normalizedCivicRegNr);
            return true;
        }

        public bool Equals(CivicRegNumberFi other)
        {
            if (other == null)
                return base.Equals(other);
            return this.normalizedValue.Equals(other.normalizedValue);
        }

        public int CompareTo(CivicRegNumberFi other)
        {
            if (other == null)
                return -1;
            else
                return this.normalizedValue.CompareTo(other.normalizedValue);
        }
    }
}
