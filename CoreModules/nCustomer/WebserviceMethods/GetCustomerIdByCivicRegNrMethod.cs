using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods
{
    public class GetCustomerIdByCivicRegNrMethod : TypedWebserviceMethod<GetCustomerIdByCivicRegNrMethod.Request, GetCustomerIdByCivicRegNrMethod.Response>
    {
        public override string Path => "CustomerIdByCivicRegNr";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ICivicRegNumber c;
            if (string.IsNullOrEmpty(request?.CivicRegNr) || !NEnv.BaseCivicRegNumberParser.TryParse(request?.CivicRegNr, out c))
                return Error("Missing or invalid civicRegNr");
            else
                return new Response
                {
                    CustomerId = CustomerIdSource.GetCustomerIdByCivicRegNr(c)
                };
        }

        public class Request
        {
            public string CivicRegNr { get; set; }
        }

        public class Response
        {
            public int? CustomerId { get; set; }
        }
    }
}