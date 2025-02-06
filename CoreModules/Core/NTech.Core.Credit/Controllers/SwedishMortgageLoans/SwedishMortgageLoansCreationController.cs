using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;

namespace NTech.Core.Credit.Controllers.SwedishMortgageLoans
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class SwedishMortgageLoansCreationController : Controller
    {
        private readonly SwedishMortageLoanCreationService swedishMortageLoanCreationService;
        private readonly MortgageLoanCollateralService mortgageLoanCollateralService;
        private readonly ICoreClock clock;
        private readonly CreditContextFactory creditContextFactory;

        public SwedishMortgageLoansCreationController(SwedishMortageLoanCreationService swedishMortageLoanCreationService, MortgageLoanCollateralService mortgageLoanCollateralService, ICoreClock clock, CreditContextFactory creditContextFactory)
        {
            this.swedishMortageLoanCreationService = swedishMortageLoanCreationService;
            this.mortgageLoanCollateralService = mortgageLoanCollateralService;
            this.clock = clock;
            this.creditContextFactory = creditContextFactory;
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Create")]
        public ActionResult<SwedishMortgageLoanCreationResponse> CreateLoans(SwedishMortgageLoanCreationRequest request)
        {
            try
            {
                return swedishMortageLoanCreationService.CreateLoans(request);
            }
            catch(MultiValidationException ex)
            {
                foreach(var error in ex.ValidationErrors)
                {
                    var name = error.MemberNames == null || error.MemberNames.Count() == 0 ? "Error" : string.Join(".", error.MemberNames);
                    ModelState.AddModelError(name, error.ErrorMessage);
                }
                return new BadRequestObjectResult(ModelState);
            }
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Calculate-AmortizationBasis")]
        public SwedishMortgageLoanAmortizationBasisModel CalculateAmortizationBasis(CalculateMortgageLoanAmortizationBasisRequest request)
        {
            return SwedishMortgageLoanAmortizationBasisService.CalculateSuggestedAmortizationBasis(request, clock.Today);
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Get-AmortizationBasis")]
        public GetSeAmortizationBasisResponse GetAmortizationBasisForExistingCredit(GetSeAmortizationBasisRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return mortgageLoanCollateralService.GetSeMortageLoanAmortizationBasis(context, request);
            }
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Current-Collateral-Loans")]
        public GetCurrentLoansOnCollateralResponse GetCurrentCollateralLoans(GetCurrentLoansOnCollateralRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return mortgageLoanCollateralService.GetCurrentLoansOnCollateral(context, request);
            }
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Get-AmortizationBasisHistory")]
        public List<SeHistoricalAmortizationBasis> GetAmortizationBasisHistory(GetAmortizationBasisHistoryRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return mortgageLoanCollateralService.GetAllHistoricalAmortizationBasis(request, context);
            }
        }
    }
}
