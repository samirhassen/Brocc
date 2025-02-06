using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods
{
    public class GetCustomerIdByOrgnrMethod : TypedWebserviceMethod<GetCustomerIdByOrgnrMethod.Request, GetCustomerIdByOrgnrMethod.Response>
    {
        public override string Path => "CustomerIdByOrgnr";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            IOrganisationNumber c;
            if (string.IsNullOrEmpty(request?.Orgnr) || !NEnv.BaseOrganisationNumberParser.TryParse(request?.Orgnr, out c))
                return Error("Missing or invalid orgnr");
            else
                return new Response
                {
                    CustomerId = CustomerIdSource.GetCustomerIdByOrgnr(c)
                };
        }

        public class Request
        {
            public string Orgnr { get; set; }
        }

        public class Response
        {
            public int? CustomerId { get; set; }
        }
    }
}