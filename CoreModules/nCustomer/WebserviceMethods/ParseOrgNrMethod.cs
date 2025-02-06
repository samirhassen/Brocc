using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods
{
    public class ParseOrgNrMethod : TypedWebserviceMethod<ParseOrgNrMethod.Request, ParseOrgNrMethod.Response>
    {
        public override string Path => "ParseOrgNr";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            IOrganisationNumber c;

            if (!NEnv.BaseOrganisationNumberParser.TryParse(request?.OrgNr, out c))
                return new Response
                {
                    IsValid = false
                };

            return new Response
            {
                IsValid = true,
                ValidNr = new Response.OrgNrModel
                {
                    NormalizedValue = c.NormalizedValue,
                    CrossCountryStorageValue = c.CrossCountryStorageValue,
                    Country = c.Country
                }
            };
        }

        public class Request
        {
            [Required]
            public string OrgNr { get; set; }
        }

        public class Response
        {
            public bool IsValid { get; set; }
            public OrgNrModel ValidNr { get; set; }
            public class OrgNrModel
            {
                public string NormalizedValue { get; set; }
                public string CrossCountryStorageValue { get; set; }
                public string Country { get; set; }
            }
        }
    }
}