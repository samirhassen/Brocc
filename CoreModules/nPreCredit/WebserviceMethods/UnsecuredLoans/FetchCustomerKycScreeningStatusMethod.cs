using NTech;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nPreCredit.WebserviceMethods
{
    public class FetchCustomerKycScreeningStatusMethod : TypedWebserviceMethod<FetchCustomerKycScreeningStatusMethod.Request, FetchCustomerKycScreeningStatusMethod.Response>
    {
        public override string Path => "Kyc/FetchCustomerScreeningStatus";

        public override bool IsEnabled => !NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.CustomerId);
            });

            var c = requestContext.Resolver().Resolve<ICustomerClient>();

            var result = c.LegacyIsCustomerScreened(request.CustomerId);

            return new Response
            {
                CustomerId = request.CustomerId,
                LatestScreeningDate = DateOnly.Create(result.Item1 ? result.Item2 : new DateTime?())
            };
        }

        public class Response
        {
            public int CustomerId { get; set; }
            public DateOnly LatestScreeningDate { get; set; }
        }

        public class Request
        {
            public int CustomerId { get; set; }
        }
    }
}