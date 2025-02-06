using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers.SwedishMortgageLoans
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class SwedishMortgageLoansRevaluateController : Controller
    {
        private readonly MlStandardSeRevaluationService revaluationService;

        public SwedishMortgageLoansRevaluateController(MlStandardSeRevaluationService revaluationService)
        {
            this.revaluationService = revaluationService;
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Compute-Revaluate")]
        public MlStandardSeRevaluationCalculateModelResponse ComputeRevaluate(MlStandardSeRevaluationCalculateRequest request) =>
            revaluationService.CalculateRevaluate(request);

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Commit-Revaluate")]
        public BusinessEventOnlyResponse CommitRevaluate(MlStandardSeRevaluationCommitRequest request) =>
            revaluationService.CommitRevaluate(request);

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Set-AmortizationExceptions")]
        public BusinessEventOnlyResponse SetAmortizationExceptions(MlStandardSeSetAmortizationExceptionsRequest request) =>
            revaluationService.SetAmortizationExceptions(request);
    }
}
