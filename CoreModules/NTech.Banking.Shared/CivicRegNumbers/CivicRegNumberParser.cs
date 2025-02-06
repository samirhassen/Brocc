using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Banking.CivicRegNumbers.Se;
using System;

namespace NTech.Banking.CivicRegNumbers
{
    public class CivicRegNumberParser
    {
        private string country;

        public CivicRegNumberParser(string country)
        {
            this.country = country;
        }

        public bool IsValid(string value)
        {
            ICivicRegNumber _;
            return TryParse(value, out _);
        }

        public ICivicRegNumber Parse(string value)
        {
            ICivicRegNumber c;

            if (!TryParse(value, out c))
                throw new ArgumentException("Invalid civicRegNr", "value");
            return c;
        }

        public bool TryParse(string value, out ICivicRegNumber civicRegNr)
        {
            if (this.country == "FI")
            {
                CivicRegNumberFi c;
                if (CivicRegNumberFi.TryParse(value, out c))
                {
                    civicRegNr = c;
                    return true;
                }
                else
                {
                    civicRegNr = null;
                    return false;
                }
            }
            else if (this.country == "SE")
            {
                CivicRegNumberSe c;
                if (CivicRegNumberSe.TryParse(value, out c))
                {
                    civicRegNr = c;
                    return true;
                }
                else
                {
                    civicRegNr = null;
                    return false;
                }
            }
            else
                throw new NotImplementedException();
        }
    }
}
