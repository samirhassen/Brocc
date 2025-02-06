using Microsoft.AspNetCore.Mvc;
using NTech.Core.PreCredit.Shared.Services;

namespace NTech.Core.PreCredit.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAny = new[] { "ntech.feature.unsecuredloans.standard", "ntech.feature.mortgageloans.standard" })]
    public class PolicyFilterController : Controller
    {
        private readonly PolicyFilterService policyFilterService;

        public PolicyFilterController(PolicyFilterService policyFilterService)
        {
            this.policyFilterService = policyFilterService;            
        }

        [HttpPost]
        [Route("Api/PreCredit/PolicyFilters/Score-Direct")]
        public DirectPolicyFilterScoringResponse ScoreDirect(DirectPolicyFilterScoringRequest request) => 
            policyFilterService.ScoreDirect(request);

        /// <summary>
        /// Exposed anonymously to the public web application for pre screening. Uses the policy filter WebPreScore if present otherwise accepts all applications.
        /// </summary>
        [HttpPost]
        [Route("Api/PreCredit/PolicyFilters/PreScore-WebApplication")]
        public WebPreScorePolicyFilterResponse ScoreDirect(WebPreScorePolicyFilterRequest request) =>
            policyFilterService.PreScoreWebApplication(request);

        /// <summary>
        /// Import a ruleset using a code of the form S_[code]_S exported from the policy filter ui.
        /// </summary>
        [HttpPost]
        [Route("Api/PreCredit/PolicyFilters/Import-RuleSet")]
        public ImportPolicyFilterResponse Import(ImportPolicyFilterRequest request) =>
            policyFilterService.ImportPolicyFilter(request);
    }
}
