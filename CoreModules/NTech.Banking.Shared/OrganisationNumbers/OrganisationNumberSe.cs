using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.OrganisationNumbers
{
    public class OrganisationNumberSe : IOrganisationNumber, IEquatable<OrganisationNumberSe>, IComparable<OrganisationNumberSe>
    {
        private readonly string longNormalizedValue; //With 16|19|20 prefix

        public string CrossCountryStorageValue
        {
            get
            {
                return $"{this.Country}{this.longNormalizedValue}";
            }
        }

        public override bool Equals(Object other)
        {
            if (other == null)
                return false;

            OrganisationNumberSe o = other as OrganisationNumberSe;
            if (o == null)
                return false;
            else
                return Equals(o);
        }

        public bool Equals(OrganisationNumberSe other)
        {
            if (other == null)
                return base.Equals(other);
            return this.CrossCountryStorageValue.Equals(other.CrossCountryStorageValue);
        }

        public int CompareTo(OrganisationNumberSe other)
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
                return this.longNormalizedValue.Substring(2);
            }
        }

        public string Country
        {
            get
            {
                return "SE";
            }
        }


        private OrganisationNumberSe(string longNormalizedValue)
        {
            this.longNormalizedValue = longNormalizedValue;
        }

        public static bool IsValid(string value)
        {
            OrganisationNumberSe _;
            return TryParse(value, out _);
        }

        public static OrganisationNumberSe Parse(string value)
        {
            OrganisationNumberSe c;
            if (!OrganisationNumberSe.TryParse(value, out c))
                throw new ArgumentException("Invalid orgnr", "value");
            return c;
        }

        public static bool TryParse(string value, out OrganisationNumberSe orgnr)
        {
            var cleanedNr = new string((value ?? "").Where(Char.IsDigit).ToArray());

            orgnr = null;

            if (cleanedNr.Length != 10 && cleanedNr.Length != 12)
                return false;

            int monthNr = cleanedNr.Length == 10 ? int.Parse(cleanedNr.Substring(2, 2)) : int.Parse(cleanedNr.Substring(4, 2));

            if(monthNr < 20)
            {
                //För att skilja på personnummer och organisationsnummer är alltid "månaden" (andra paret, som byggs upp av tredje och fjärde siffran) i ett organisationsnummer minst 20.
                CivicRegNumberSe cn;
                if (!CivicRegNumberSe.TryParse(cleanedNr, out cn))
                    return false;
                orgnr = new OrganisationNumberSe(cn.NormalizedValue);
                return true;                 
            }

            if (cleanedNr.Length == 10)
            {
                cleanedNr = $"16{cleanedNr}";
            }

            if (ComputeMod10CheckDigit(cleanedNr.Substring(2, 9)).ToString() != cleanedNr.Substring(cleanedNr.Length - 1, 1))
                return false;

            orgnr = new OrganisationNumberSe(cleanedNr);
            return true;
        }

        internal static int ComputeMod10CheckDigit(string input)
        {
            return (10 - (input
                .Reverse()
                .Select((x, i) => (int.Parse(new string(new[] { x })) * (i % 2 == 0 ? 2 : 1)))
                .Sum(x => (x % 10) + (x >= 10 ? 1 : 0)) % 10)) % 10;
        }
    }
}
