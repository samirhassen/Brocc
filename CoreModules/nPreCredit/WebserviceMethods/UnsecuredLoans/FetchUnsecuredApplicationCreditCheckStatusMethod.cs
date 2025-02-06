using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchUnsecuredApplicationCreditCheckStatusMethod : TypedWebserviceMethod<FetchUnsecuredApplicationCreditCheckStatusMethod.Request, FetchUnsecuredApplicationCreditCheckStatusMethod.Response>
    {
        public override string Path => "UnsecuredApplication/FetchCreditCheckStatus";

        public override bool IsEnabled => !NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.ApplicationNr);
            });

            var u = requestContext.Resolver().Resolve<IHttpContextUrlService>();
            var handler = DependancyInjection.Services.Resolve<ICreditApplicationTypeHandler>();

            using (var context = new PreCreditContext())
            {
                IQueryable<CreditDecision> q;
                if (request.IncludePauseItems.GetValueOrDefault())
                    q = context.CreditDecisions.Include("PauseItems");
                else
                    q = context.CreditDecisions;

                var d = q
                    .Where(x => x.ApplicationNr == request.ApplicationNr && x.Id == x.CreditApplication.CurrentCreditDecisionId)
                    .SingleOrDefault();

                return new Response
                {
                    ApplicationNr = request.ApplicationNr,
                    NewCreditCheckUrl = u.ActionStrict("NewCreditCheck", "CreditManagement", new { applicationNr = request.ApplicationNr }),
                    ViewCreditDecisionUrl = d == null
                                ? null
                                : handler.GetViewCreditDecisionUrl(requestContext.Resolver().Resolve<IServiceRegistryUrlService>(), d.Id),
                    CurrentCreditDecision = d?.ToJsModel(request.IncludePauseItems.GetValueOrDefault(), false),
                    RejectionReasonDisplayNames = handler.GetRejectionReasonToDisplayNameMapping().Select(x => new RejectionReason { Name = x.Key, Value = x.Value }).ToList()
                };
            }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public string NewCreditCheckUrl { get; set; }
            public string ViewCreditDecisionUrl { get; set; }
            public CreditDecision.CreditDecisionJsModel CurrentCreditDecision { get; set; }
            public List<RejectionReason> RejectionReasonDisplayNames { get; set; }
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public bool? IncludePauseItems { get; set; }
            public bool? IncludeRejectionReasonDisplayNames { get; set; }
        }

        public class RejectionReason
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}