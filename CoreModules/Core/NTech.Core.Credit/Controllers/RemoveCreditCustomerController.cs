using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.PreCredit.Apis
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAny = new[] { "ntech.feature.unsecuredloans", "ntech.feature.mortgageloans" })]
    public class RemoveCreditCustomerController : Controller
    {
        private readonly ChangeCreditCustomersService changeCreditCustomersService;

        public RemoveCreditCustomerController(ChangeCreditCustomersService changeCreditCustomersService)
        {
            this.changeCreditCustomersService = changeCreditCustomersService;
        }

        [HttpPost]
        [Route("Api/Credit/CreditCustomer/Remove")]
        public RemoveCreditCustomerResponse RemoveCreditCustomer(RemoveCreditCustomerRequest request)
        {
            return changeCreditCustomersService.RemoveCreditCustomer(request);
        }
    }
}
