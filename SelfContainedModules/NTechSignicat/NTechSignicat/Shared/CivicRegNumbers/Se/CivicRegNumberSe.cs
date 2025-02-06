using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.CivicRegNumbers.Se
{
    public class CivicRegNumberSe : CivicRegNumberBase,
        IEquatable<CivicRegNumberSe>, IComparable<CivicRegNumberSe>
    {
        private readonly string normalizedValue;
        public static Func<DateTime> Today = () => DateTime.Today;
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
                return "SE";
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
                return (int.Parse(this.normalizedValue.Substring(this.normalizedValue.Length - 2, 1)) % 2) != 0;
            }
        }

        private CivicRegNumberSe(string normalizedValue)
        {
            this.normalizedValue = normalizedValue;
        }

        public static bool IsValid(string value)
        {
            CivicRegNumberSe _;
            return TryParse(value, out _);
        }

        private static DateTime? ParseBirthDate(string civicRegNr)
        {
            DateTime dateValue;
            if (!DateTime.TryParseExact(civicRegNr.Substring(0, 8), "yyyyMMdd", null, DateTimeStyles.None, out dateValue))
                return null;
            return dateValue;
        }

        public static CivicRegNumberSe Parse(string value)
        {
            CivicRegNumberSe c;
            if (!CivicRegNumberSe.TryParse(value, out c))
                throw new ArgumentException("Invalid civicRegNr", "value");
            return c;
        }

        public static bool TryParse(string value, out CivicRegNumberSe civicRegNr)
        {
            //[YY]YYMMDDCCCX
            var cleanedNr = new string((value ?? "").Where(Char.IsDigit).ToArray());

            civicRegNr = null;

            if (cleanedNr.Length != 10 && cleanedNr.Length != 12)
                return false;

            if (cleanedNr.Length == 10)
            {
                //If guessing 20 would put their birthyear in the future use 19, otherwise use 20
                var yearIf20 = int.Parse("20" + cleanedNr.Substring(0, 2));
                if (yearIf20 > Today().Year)
                    cleanedNr = $"19{cleanedNr}";
                else
                    cleanedNr = $"20{cleanedNr}";
            }

            var date = ParseBirthDate(cleanedNr.Substring(0, 8));
            if (date == null)
                return false;

            if (ComputeMod10CheckDigit(cleanedNr.Substring(2, 9)).ToString() != cleanedNr.Substring(cleanedNr.Length - 1, 1))
                return false;

            civicRegNr = new CivicRegNumberSe(cleanedNr);
            return true;
        }

        internal static int ComputeMod10CheckDigit(string input)
        {
            return (10 - (input
                .Reverse()
                .Select((x, i) => (int.Parse(new string(new[] { x })) * (i % 2 == 0 ? 2 : 1)))
                .Sum(x => (x % 10) + (x >= 10 ? 1 : 0)) % 10)) % 10;
        }


        public bool Equals(CivicRegNumberSe other)
        {
            if (other == null)
                return base.Equals(other);
            return this.normalizedValue.Equals(other.normalizedValue);
        }

        public int CompareTo(CivicRegNumberSe other)
        {
            if (other == null)
                return -1;
            else
                return this.normalizedValue.CompareTo(other.normalizedValue);
        }
    }
}
