using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace nCredit.WebserviceMethods
{
    public class BulkFetchCustomerPropertiesMethod : TypedWebserviceMethod<BulkFetchCustomerPropertiesMethod.Request, BulkFetchCustomerPropertiesMethod.Response>
    {
        public override string Path => "Customer/Bulk-Fetch-Properties";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var cl = new Code.CreditCustomerClient();

            var result = cl.BulkFetchPropertiesByCustomerIdsD(request.CustomerIds.ToHashSet(), request.PropertyNames.Distinct().ToArray());
            return new Response
            {
                Properties = result
            };
        }

        public class Request
        {
            [Required]
            public List<int> CustomerIds { get; set; }

            [Required]
            public List<string> PropertyNames { get; set; }
        }

        public class Response
        {
            public Dictionary<int, Dictionary<string, string>> Properties { get; set; }
        }
    }
}