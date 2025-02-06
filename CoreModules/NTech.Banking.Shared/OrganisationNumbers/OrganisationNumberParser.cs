using NTech.Banking.OrganisationNumbers;
using System;

namespace NTech.Banking.OrganisationNumbers
{
    public class OrganisationNumberParser
    {
        private string country;

        public OrganisationNumberParser(string country)
        {
            this.country = country;
        }

        public bool IsValid(string value)
        {
            IOrganisationNumber _;
            return TryParse(value, out _);
        }

        public IOrganisationNumber Parse(string value)
        {
            IOrganisationNumber c;

            if (!TryParse(value, out c))
                throw new ArgumentException("Invalid organisationNumber", "value");
            return c;
        }

        public bool TryParse(string value, out IOrganisationNumber orgNr)
        {
            if (this.country == "SE")
            {
                OrganisationNumberSe c;
                if (OrganisationNumberSe.TryParse(value, out c))
                {
                    orgNr = c;
                    return true;
                }
                else
                {
                    orgNr = null;
                    return false;
                }
            }
            else if(this.country == "FI")
            {
                OrganisationNumberFi c;
                if (OrganisationNumberFi.TryParse(value, out c))
                {
                    orgNr = c;
                    return true;
                }
                else
                {
                    orgNr = null;
                    return false;
                }
            }
            else
                throw new NotImplementedException();
        }
    }
}
