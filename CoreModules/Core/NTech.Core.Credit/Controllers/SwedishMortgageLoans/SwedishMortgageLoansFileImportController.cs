using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;

namespace NTech.Core.Credit.Controllers.SwedishMortgageLoans
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class SwedishMortgageLoansFileImportController : Controller
    {
        private readonly SwedishMortgageLoanImportService service;

        public SwedishMortgageLoansFileImportController(SwedishMortgageLoanImportService service)
        {
            this.service = service;
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Import-Excel-File")]
        public SwedishMortgageLoanImportResponse ImportExcelFile(SwedishMortgageLoanImportRequest request)
        {
            return service.ImportFile(request);
        }
    }
}
