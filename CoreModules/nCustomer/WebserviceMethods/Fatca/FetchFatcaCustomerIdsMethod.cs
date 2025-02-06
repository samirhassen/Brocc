using nCustomer.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class FetchFatcaCustomerIdsMethod : ContactInfoMethodBase<FetchFatcaCustomerIdsMethod.Request, FetchFatcaCustomerIdsMethod.Response>
    {
        public override string Path => "Fatca/FetchCustomerIds";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            using (var db = new CustomersContext())
            {
                var customerIds = db
                    .CustomerProperties
                    .Where(x => x.IsCurrentData && x.Name == CustomerProperty.Codes.includeInFatcaExport.ToString() && x.Value == "true")
                    .Select(x => x.CustomerId)
                    .ToList();

                return new Response
                {
                    CustomerIds = customerIds
                };
            }
        }

        public class Response
        {
            public List<int> CustomerIds { get; set; }
        }

        public class Request
        {

        }
    }
}