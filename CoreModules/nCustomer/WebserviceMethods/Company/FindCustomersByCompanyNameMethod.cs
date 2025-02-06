using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods.Company
{
    public class FindCustomersByCompanyNameMethod : TypedWebserviceMethod<FindCustomersByCompanyNameMethod.Request, FindCustomersByCompanyNameMethod.Response>
    {
        public override string Path => "CompanyCustomer/FindByName";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            List<string> debugData = null;
            Action<string> logDebugData = null;
            if (request.IncludeDebugData.GetValueOrDefault())
            {
                debugData = new List<string>();
                logDebugData = debugData.Add;
            }

            var customerIds = requestContext.Service().CompanyLoanNameSearch.FindCustomerByCompanyName(request.NameFragment, logDebugData: logDebugData).ToList();
            var r = new Response
            {
                CustomerIds = customerIds,
                DebugData = debugData
            };

            if (customerIds.Any() && request.ReturnProperties != null && request.ReturnProperties.Count > 0)
            {
                r.ReturnedProperties = requestContext.Service().Customer
                    .BulkFetch(customerIds.ToHashSet(), request.ReturnProperties.ToHashSet(), requestContext.CurrentUserMetadata())
                    .ToDictionary(
                    x => x.Key,
                    x => x.Value.GroupBy(y => y.Name).ToDictionary(y => y.Key, y => y.First().Value));
            }

            return r;
        }

        public class Request
        {
            [Required]
            public string NameFragment { get; set; }
            public List<string> ReturnProperties { get; set; }
            public bool? IncludeDebugData { get; set; }
        }

        public class Response
        {
            public List<int> CustomerIds { get; set; }
            public Dictionary<int, Dictionary<string, string>> ReturnedProperties { get; set; }
            public List<string> DebugData { get; set; }
        }
    }
}