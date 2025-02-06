using nCustomerPages.Code;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    public class FetchMortgageLoanWebappSettingsController : EmbeddedCustomerPagesControllerBase
    {
        protected override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        [Route("api/embedded-customerpages/fetch-ml-webapp-settings")]
        [AllowAnonymous]
        public ActionResult FetchMortgageLoanWebappSettings()
        {
            return new JsonNetActionResult
            {
                Data = new
                {
                    MortgageLoanExternalApplicationSettings = new SystemUserCustomerClient().LoadSettings("mortgageLoanExternalApplication")
                }
            };
        }
    }
}