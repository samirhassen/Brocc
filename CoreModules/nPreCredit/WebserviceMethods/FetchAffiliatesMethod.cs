using NTech.Core.Module;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nPreCredit.WebserviceMethods
{
    public class FetchAffiliatesMethod : TypedWebserviceMethod<FetchAffiliatesMethod.Request, FetchAffiliatesMethod.Response>
    {
        public override string Path => "Affiliates/FetchAll";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            return new Response
            {
                Affiliates = NEnv.GetAffiliateModels()
            };
        }

        public class Request
        {

        }

        public class Response
        {
            public List<AffiliateModel> Affiliates { get; set; }
        }
    }
}