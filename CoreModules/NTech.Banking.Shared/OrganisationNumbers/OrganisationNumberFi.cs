using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.OrganisationNumbers
{
    public class OrganisationNumberFi : IOrganisationNumber, IEquatable<OrganisationNumberFi>, IComparable<OrganisationNumberFi>
    {
        private readonly string normalizedValue;

        public string CrossCountryStorageValue
        {
            get
            {
                return $"{this.Country}{this.NormalizedValue}";
            }
        }

        public override bool Equals(Object other)
        {
            if (other == null)
                return false;

            OrganisationNumberFi o = other as OrganisationNumberFi;
            if (o == null)
                return false;
            else
                return Equals(o);
        }

        public bool Equals(OrganisationNumberFi other)
        {
            if (other == null)
                return base.Equals(other);
            return this.CrossCountryStorageValue.Equals(other.CrossCountryStorageValue);
        }

        public int CompareTo(OrganisationNumberFi other)
        {
            if (other == null)
                return -1;
            else
                return this.CrossCountryStorageValue.CompareTo(other.CrossCountryStorageValue);
        }

        public override int GetHashCode()
        {
            return this.CrossCountryStorageValue.GetHashCode();
        }

        public string NormalizedValue
        {
            get
            {
                return this.normalizedValue;
            }
        }

        public string Country
        {
            get
            {
                return "FI";
            }
        }


        private OrganisationNumberFi(string normalizedValue)
        {
            this.normalizedValue = normalizedValue;
        }

        public static bool IsValid(string value)
        {
            OrganisationNumberFi _;
            return TryParse(value, out _);
        }

        public static OrganisationNumberFi Parse(string value)
        {
            OrganisationNumberFi c;
            if (!OrganisationNumberFi.TryParse(value, out c))
                throw new ArgumentException("Invalid y-tunnus", "value");
            return c;
        }

        public static bool TryParse(string value, out OrganisationNumberFi orgnr)
        {
            //Format: AABBCCD-E
            orgnr = null;

            var cleanedNr = new string((value ?? "").Where(x => Char.IsDigit(x) || x == '-').ToArray());

            if (cleanedNr.Length < 9)
                cleanedNr = cleanedNr.PadLeft(9, '0'); //Some old nrs are 6 digits

            if (cleanedNr[7] != '-' || cleanedNr.Count(x => x == '-') != 1)
                return false;

            var checkDigit = int.Parse(cleanedNr.Substring(8, 1));

            int computedCheckDigit;
            if (!TryComputeCheckDigit(cleanedNr.Substring(0, 7), out computedCheckDigit))
                return false;

            if (checkDigit != computedCheckDigit)
                return false;

            orgnr = new OrganisationNumberFi(cleanedNr);

            return true;
        }

        private static int[] ChecksumWeights = new int[] { 7, 9, 10, 5, 8, 4, 2 };

        public static bool TryComputeCheckDigit(string input, out int checkDigit)
        {
            checkDigit = -1;

            var c = 0;
            for(var i=0; i<ChecksumWeights.Length; i++)
            {
                c += ChecksumWeights[i] * int.Parse(input.Substring(i, 1));
            }
            var r = c % 11;

            if (r == 1)
                return false;

            if (r == 0)
                checkDigit = 0;
            else
                checkDigit = 11 - r;

            return true;
        }
    }
}
