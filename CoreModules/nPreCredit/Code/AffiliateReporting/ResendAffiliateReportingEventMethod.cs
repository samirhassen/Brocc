using NTech.Services.Infrastructure.NTechWs;

namespace nPreCredit.Code.AffiliateReporting
{
    public class ResendAffiliateReportingEventMethod : TypedWebserviceMethod<ResendAffiliateReportingEventMethod.Request, ResendAffiliateReportingEventMethod.Response>
    {
        public override string Path => "AffiliateReporting/Events/Resend";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Id);
            });

            var resolver = requestContext
                .Resolver();

            var s = resolver
                .Resolve<IAffiliateReportingService>();

            if (!s.TryResetEventToPending(request.Id.Value))
                return Error("No such event exists", httpStatusCode: 400, errorCode: "noSuchEventExists");

            return new Response();
        }

        public class Request
        {
            public long? Id { get; set; }
        }

        public class Response
        {

        }
    }
}