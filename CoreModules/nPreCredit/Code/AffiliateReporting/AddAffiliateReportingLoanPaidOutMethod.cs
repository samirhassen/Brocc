using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AddAffiliateReportingLoanPaidOutMethod : TypedWebserviceMethod<AddAffiliateReportingLoanPaidOutMethod.Request, AddAffiliateReportingLoanPaidOutMethod.Response>
    {
        public override string Path => "AffiliateReporting/Events/AddLoanPaidOut";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = requestContext
                .Resolver()
                .Resolve<IAffiliateReportingService>();
            var ids = s.AddLoanPaidOutEventModels(request.Events);

            return new Response
            {
                Ids = ids
            };
        }

        public class Request
        {
            public List<LoanPaidOutEventModel> Events { get; set; }
        }

        public class Response
        {
            public List<long> Ids { get; set; }
        }
    }
}