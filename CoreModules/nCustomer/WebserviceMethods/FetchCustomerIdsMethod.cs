using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCustomer.WebserviceMethods
{
    public class FetchCustomerIdsMethod : TypedWebserviceMethod<FetchCustomerIdsMethod.Request, FetchCustomerIdsMethod.Response>
    {
        public override string Path => "FetchCustomerIds";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var r = new Response();
            using (var context = new DbModel.CustomersContext())
            {
                if (request.CivicRegNrs != null)
                {
                    r.CivicRegNrCustomerIds = new List<int>();
                    foreach (var cRaw in request.CivicRegNrs)
                    {
                        if (string.IsNullOrEmpty(cRaw) || !NEnv.BaseCivicRegNumberParser.TryParse(cRaw, out var cNr))
                            return Error("Missing or invalid civicRegNr");
                        r.CivicRegNrCustomerIds.Add(CustomerIdSource.GetCustomerIdByCivicRegNr(cNr, context: context));
                    }
                }
                if (request.OrgNrs != null)
                {
                    r.OrgNrCustomerIds = new List<int>();
                    foreach (var oRaw in request.OrgNrs)
                    {
                        if (string.IsNullOrEmpty(oRaw) || !NEnv.BaseOrganisationNumberParser.TryParse(oRaw, out var oNr))
                            return Error("Missing or invalid orgnr");
                        r.OrgNrCustomerIds.Add(CustomerIdSource.GetCustomerIdByOrgnr(oNr, context: context));
                    }
                }
            }

            return r;
        }

        public class Request
        {
            public List<string> CivicRegNrs { get; set; }
            public List<string> OrgNrs { get; set; }
        }

        public class Response
        {
            public List<int> CivicRegNrCustomerIds { get; set; }
            public List<int> OrgNrCustomerIds { get; set; }
        }
    }
}