using nPreCredit.Code.Services.CompanyLoans;
using NTech.Core.Module;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanWorkflowStepNamesMethod : TypedWebserviceMethod<FetchCompanyLoanWorkflowStepNamesMethod.Request, FetchCompanyLoanWorkflowStepNamesMethod.Response>
    {
        public override string Path => "CompanyLoan/Fetch-WorkflowStepNames";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var c = requestContext.Resolver().Resolve<ICompanyLoanWorkflowService>();

            var r = new Response
            {
                StepNames = c.GetStepOrder()
            };

            if (request.IncludeAffiliates.GetValueOrDefault())
                r.Affiliates = NEnv.GetAffiliateModels();

            return r;
        }

        public class Request
        {
            public bool? IncludeAffiliates { get; set; }
        }

        public class Response
        {
            public List<string> StepNames { get; set; }
            public List<AffiliateModel> Affiliates { get; set; }
        }
    }
}