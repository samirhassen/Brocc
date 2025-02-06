using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Credit.Controllers.SwedishMortgageLoans
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class MortgageLoanPropertyIdController
    {
        private readonly CreditContextFactory creditContextFactory;

        public MortgageLoanPropertyIdController(CreditContextFactory creditContextFactory)
        {
            this.creditContextFactory = creditContextFactory;
        }

        [HttpPost]
        [Route("Api/Credit/MortgageLoan/Property-Id-By-CollateralId")]
        public Dictionary<int, string> PropertyIdsByCollateralId(MortgageLoanPropertyIdRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return MortgageLoanCollateralService.GetPropertyIdByCollateralId(context, request.CollateralIds.ToHashSet(), !(request.ExcludeObjectTypeLabel ?? false));
            }
        }

    }

    public class MortgageLoanPropertyIdRequest
    {
        [Required]
        public List<int> CollateralIds { get; set; }
        public bool? ExcludeObjectTypeLabel { get; set; }
    }
}
