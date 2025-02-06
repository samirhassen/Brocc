using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.AffiliateReporting
{
    public class FetchAffiliateReportingEventsForApplicationMethod : TypedWebserviceMethod<FetchAffiliateReportingEventsForApplicationMethod.Request, FetchAffiliateReportingEventsForApplicationMethod.Response>
    {
        public override string Path => "AffiliateReporting/Events/FetchAllForApplication";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.ApplicationNr);
            });

            var resolver = requestContext
                .Resolver();

            var s = resolver
                .Resolve<IAffiliateReportingService>();

            var r = new Response
            {
                Events = s.GetAffiliateReportingEventsForApplication(request.ApplicationNr)
            };

            if (!request.IncludeAffiliateMetadata.GetValueOrDefault())
                return r;

            var providerName = PreCreditContext.WithContext(x => x
                    .CreditApplicationHeaders
                    .Select(y => new { y.ProviderName, y.ApplicationNr })
                    .Single(y => y.ApplicationNr == request.ApplicationNr)
                    .ProviderName);

            var ds = resolver.Resolve<IAffiliateDataSource>();

            r.AffiliateMetadata = new AffiliateMetadataModel
            {
                HasDispatcher = ds.GetDispatcher(providerName) != null
            };

            return r;
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public bool? IncludeAffiliateMetadata { get; set; }
        }

        public class Response
        {
            public List<AffiliateReportingEventModel> Events { get; set; }

            public AffiliateMetadataModel AffiliateMetadata { get; set; }
        }

        public class AffiliateMetadataModel
        {
            public bool HasDispatcher { get; set; }
        }
    }
}