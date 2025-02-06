using nCustomer.Code.Services.Kyc;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCustomer.WebserviceMethods
{
    public class FetchCustomerRelationsMethod : TypedWebserviceMethod<FetchCustomerRelationsMethod.Request, FetchCustomerRelationsMethod.Response>
    {
        public override string Path => "CustomerRelations/FetchForCustomer";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(r => r.CustomerId);
            });

            return new Response
            {
                CustomerRelations = requestContext.Service().KycManagement.FetchCustomerRelations(request.CustomerId.Value)
            };
        }

        public class Request
        {
            public int? CustomerId { get; set; }
        }

        public class Response
        {
            public List<CustomerRelationModel> CustomerRelations { get; set; }
        }
    }
}