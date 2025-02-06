using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class CustomerKycScreenMethod : TypedWebserviceMethod<CustomerKycScreenMethod.Request, CustomerKycScreenMethod.Response>
    {
        public override string Path => "Kyc/ScreenCustomer";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.CustomerId);
            });

            var c = requestContext.Resolver().Resolve<ICustomerClient>();

            var result = c.KycScreenNew(
                new[] { request.CustomerId }.ToHashSet(),
                requestContext.Clock().Today, true);
            var failedReason = result.Opt(request.CustomerId);

            return new Response
            {
                Success = failedReason == null,
                Skipped = failedReason != null,
                FailureCode = failedReason
            };
        }

        public class Response
        {
            public bool Success { get; set; }
            public bool Skipped { get; set; }
            public string FailureCode { get; set; }
        }

        public class Request
        {
            public int CustomerId { get; set; }
            public bool? Force { get; set; }
        }
    }
}