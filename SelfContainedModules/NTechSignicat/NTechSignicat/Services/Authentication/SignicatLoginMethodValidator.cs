using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public class SignicatLoginMethodValidator
    {
        public bool TryValidate(string expectedCivicRegNr,
                    List<string> loginMethods, out ValidatedParameters validatedParameters)
        {
            var p = new ValidatedParameters();

            validatedParameters = null;

            if (loginMethods == null || loginMethods.Count == 0)
                return false;

            if (string.IsNullOrWhiteSpace(expectedCivicRegNr))
                return false;

            validatedParameters = null;
            var validMethods = new List<SignicatLoginMethodCode>();
            foreach (var m in (loginMethods ?? new List<string>()))
            {
                SignicatLoginMethodCode c;
                if (!Enum.TryParse(m, out c))
                    return false;
                validMethods.Add(c);
            }

            p.LoginMethods = validMethods;

            var countries = validMethods.Select(MapCountryFromMethod).Distinct().ToList();
            if (countries.Count > 1)
                return false;

            if (!string.IsNullOrWhiteSpace(expectedCivicRegNr))
            {
                if (countries.Count != 1)
                    return false;
                var v = new CivicRegNumberParser(countries.Single()).Parse(expectedCivicRegNr, returnNullIfInvalid: true);
                if (v == null)
                    return false;
                p.ExpectedCivicRegNr = v;
            }

            validatedParameters = p;
            return true;
        }

        public static ICivicRegNumber ParseCivicRegNr(string countryCode, string civicRegNr)
        {
            return new CivicRegNumberParser(countryCode).Parse(civicRegNr);
        }

        private string MapCountryFromMethod(SignicatLoginMethodCode signicatLoginMethodCode)
        {
            switch (signicatLoginMethodCode)
            {
                case SignicatLoginMethodCode.FinnishTrustNetwork:
                    return "FI";

                case SignicatLoginMethodCode.SwedishBankId:
                    return "SE";

                default:
                    throw new NotImplementedException();
            }
        }

        public class ValidatedParameters
        {
            public ICivicRegNumber ExpectedCivicRegNr { get; set; }
            public List<SignicatLoginMethodCode> LoginMethods { get; set; }
        }
    }
}