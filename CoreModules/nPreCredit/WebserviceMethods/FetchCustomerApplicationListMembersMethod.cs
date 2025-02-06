using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class FetchCustomerApplicationListMembersMethod : TypedWebserviceMethod<FetchCustomerApplicationListMembersMethod.Request, FetchCustomerApplicationListMembersMethod.Response>
    {
        public override string Path => "ApplicationCustomerList/Fetch-Members";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var customerIds = resolver.Resolve<CreditApplicationCustomerListService>().GetMemberCustomerIds(request.ApplicationNr, request.ListName);

            return new Response { CustomerIds = customerIds };
        }

        public class Response
        {
            public List<int> CustomerIds { get; set; }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string ListName { get; set; }
        }
    }
}