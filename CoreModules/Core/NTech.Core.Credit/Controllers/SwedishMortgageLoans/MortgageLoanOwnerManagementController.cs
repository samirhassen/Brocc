using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class MortgageLoanOwnerManagementController : Controller
    {
        private readonly MortageLoanOwnerManagementService loanOwnerManagementService;

        public MortgageLoanOwnerManagementController(MortageLoanOwnerManagementService loanOwnerManagementService)
        {
            this.loanOwnerManagementService = loanOwnerManagementService;
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/LoanOwnerManagement/Fetch")]
        public LoanOwnerManagementResponse FetchLoanOwnerManagement(LoanOwnerManagementRequest request)
        {
            return loanOwnerManagementService.FetchMortgageLoanOwners(request.CreditNr);
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/LoanOwnerManagement/Edit")]
        public LoanOwnerManagementResponse EditLoanOwner(LoanOwnerManagementRequest request)
        {
            return loanOwnerManagementService.EditOwner(request);
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/LoanOwnerManagement/BulkEdit")]
        public LoanOwnerManagementResponse BulkEditLoanOwner(BulkEditOwnerRequest request)
        {
            return loanOwnerManagementService.BulkEditOwner(request);
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/LoanOwnerManagement/BulkEditPreview")]
        public BulkEditLoanOwnerPreviewResponse BulkEditLoanOwnerPreview(BulkEditOwnerPreviewRequest request)
        {
            return loanOwnerManagementService.GetBulkEditPreview(request);
        }
    }
}
