using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure.MortgageLoanStandard;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    public class CreateUlStandardApplicationController : EmbeddedCustomerPagesControllerBase
    {
        protected override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled && NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.webapplication");

        [Route("api/embedded-customerpages/create-ul-standard-application")]
        [HttpPost]
        [AllowAnonymous]
        public ActionResult CreateApplication()
        {
            return SendForwardApiCall(request =>
            {
                request.RemoveJsonProperty("Meta", true);
                request.Add("Meta", JObject.FromObject(new
                {
                    ProviderName = NEnv.GetAffiliateModels().First(x => x.IsSelf).ProviderName
                }));
                return null;
            }, "nPreCredit", "Api/UnsecuredLoanStandard/Create-Application-From-CustomerPages");
        }
    }
}