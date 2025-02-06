using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure.MortgageLoanStandard;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    public class CreateMortgageLoanApplicationController : EmbeddedCustomerPagesControllerBase
    {
        protected override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        [Route("api/embedded-customerpages/create-mortgageloan-application")]
        [HttpPost]
        public ActionResult CreateApplication()
        {
            return SendForwardApiCall(request =>
            {
                var meta = request?.GetValue("Meta", StringComparison.OrdinalIgnoreCase)?.ToObject<MortgageLoanStandardApplicationCreateRequest.MetadataModel>();
                request.RemoveJsonProperty("Meta", true);
                request.Add("Meta", JObject.FromObject(new
                {
                    ProviderName = NEnv.GetAffiliateModels().First(x => x.IsSelf).ProviderName
                }));
                return null;
            }, "nPreCredit", "Api/MortgageLoanStandard/Create-Application");
        }
    }
}