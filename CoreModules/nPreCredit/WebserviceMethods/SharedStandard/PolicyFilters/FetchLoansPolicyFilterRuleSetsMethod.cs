using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nPreCredit.WebserviceMethods.SharedStandard.PolicyFilters
{
    public class FetchLoansPolicyFilterRuleSetsMethod : TypedWebserviceMethod<FetchPolicyFilterRuleSetsRequest, FetchPolicyFilterRuleSetsResponse>
    {
        public override string Path => "LoanStandard/PolicyFilters/Fetch-RuleSets";

        public override bool IsEnabled => PolicyFilterService.IsEnabled(NEnv.EnvSettings);

        protected override FetchPolicyFilterRuleSetsResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, FetchPolicyFilterRuleSetsRequest request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<PolicyFilterService>();
            return s.FetchPolicyFilterRuleSets(request);
        }
    }
}