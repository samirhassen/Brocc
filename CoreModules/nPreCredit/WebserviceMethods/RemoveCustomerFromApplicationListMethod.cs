using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class RemoveCustomerFromApplicationListMethod : TypedWebserviceMethod<RemoveCustomerFromApplicationListMethod.Request, RemoveCustomerFromApplicationListMethod.Response>
    {
        public override string Path => "ApplicationCustomerList/Remove-Customer";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            bool wasRemoved = false;
            resolver.Resolve<CreditApplicationCustomerListService>().SetMemberStatus(
                request.ListName, false, request.CustomerId.Value, applicationNr: request.ApplicationNr, observeStatusChange: x => wasRemoved = !x);

            return new Response { CustomerId = request.CustomerId.Value, WasRemoved = wasRemoved };
        }

        public class Response
        {
            public int CustomerId { get; set; }
            public bool WasRemoved { get; set; }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string ListName { get; set; }
            [Required]
            public int? CustomerId { get; set; }
        }
    }
}