using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.CivicRegNumbers
{
    public abstract class CivicRegNumberBase : ICivicRegNumber, IEquatable<ICivicRegNumber>, IComparable<ICivicRegNumber>
    {
        public abstract DateTime? BirthDate { get; }
        public abstract bool? IsMale { get; }

        public abstract string Country { get; }
        public abstract string NormalizedValue { get; }

        public string CrossCountryStorageValue
        {
            get
            {
                return $"{this.Country}{this.NormalizedValue}";
            }
        }

        public bool Equals(ICivicRegNumber other)
        {
            if (other == null)
                return base.Equals(other);
            return this.CrossCountryStorageValue.Equals(other.CrossCountryStorageValue);
        }

        public int CompareTo(ICivicRegNumber other)
        {
            if (other == null)
                return -1;
            else
                return this.CrossCountryStorageValue.CompareTo(other.CrossCountryStorageValue);
        }

        public override bool Equals(Object other)
        {
            if (other == null)
                return false;

            ICivicRegNumber o = other as ICivicRegNumber;
            if (o == null)
                return false;
            else
                return Equals(o);
        }
        
        public override int GetHashCode()
        {
            return this.CrossCountryStorageValue.GetHashCode();
        }
    }
}
