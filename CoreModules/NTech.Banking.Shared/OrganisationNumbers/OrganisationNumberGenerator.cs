using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTech.Banking.OrganisationNumbers
{
    public class OrganisationNumberGenerator
    {
        private string country;

        public OrganisationNumberGenerator(string country)
        {
            this.country = country;
        }

        public IOrganisationNumber Generate(Func<int, int, int> nextRandomNr)
        {
            if (country == "FI")
            {
                //AABBCCD-E
                Func<string> getPrefix = () =>
                    $"{nextRandomNr(100, 1000)}{nextRandomNr(1000, 10000)}";

                var i = 0;
                while(i++ < 50)
                {
                    var prefix = getPrefix();
                    int cd;
                    if (OrganisationNumberFi.TryComputeCheckDigit(prefix, out cd))
                        return OrganisationNumberFi.Parse($"{prefix}-{cd}");
                }

                throw new Exception("Could not generate a valid fi orgnr");
            }
            else if (country == "SE")
            {
                var seqNr = nextRandomNr(1, 999);
                var yy = nextRandomNr(10, 100);
                var mm = nextRandomNr(20, 100);
                var dd = nextRandomNr(10, 100);
                var nr = $"{yy}{mm}{dd}{seqNr.ToString().PadLeft(3, '0')}";
                var checkDigit = CivicRegNumberSe.ComputeMod10CheckDigit(nr).ToString();
                return OrganisationNumberSe.Parse($"{nr}{checkDigit}");
            }
            else
                throw new NotImplementedException();
        }
    }
}
