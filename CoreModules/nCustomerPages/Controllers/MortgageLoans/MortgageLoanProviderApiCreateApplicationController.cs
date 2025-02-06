using Newtonsoft.Json.Linq;
using System;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class MortgageLoanProviderApiCreateApplicationController : MortgageLoanProviderApiBaseController
    {
        [Route("api/v1/mortgage-loan/create-application")]
        [HttpPost]
        public ActionResult CreateApplication()
        {
            return WithRequestAsJObject(this.Request.Url.PathAndQuery, requestObject =>
            {
                var meta = requestObject?.GetValue("Meta", StringComparison.OrdinalIgnoreCase)?.ToObject<ApplicationMetadataModel>();
                requestObject.RemoveJsonProperty("Meta", true);
                requestObject.Add("Meta", JObject.FromObject(new
                {
                    ProviderName = CurrentProviderName,
                    DisableAutomation = (bool?)null,
                    CustomerExternalIpAddress = meta?.CustomerExternalIpAddress
                }));

                return ForwardApiRequest("nPreCredit", "api/mortgageloan/create-application", requestObject);
            });
        }

        private class ApplicationMetadataModel
        {
            public string ProviderName { get; set; }
            public bool? DisableAutomation { get; set; }
            public string CustomerExternalIpAddress { get; set; }
        }
    }
}