using Newtonsoft.Json.Linq;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class ConsumerCreditStandardApiCreateApplicationController : ConsumerCreditStandardProviderApiBaseController
    {
        [Route("api/v1/unsecured-loan-standard/create-application")]
        [HttpPost]
        public ActionResult CreateApplication()
        {
            return WithRequestAsJObject(this.Request.Url.PathAndQuery, requestObject =>
            {
                //TODO: Do we allow everyone to send this or just trusted providers?
                var customerExternalIpAddress = requestObject.GetStringPropertyValue("CustomerExternalIpAddress", true);
                requestObject.Remove("CustomerExternalIpAddress");

                //TODO: Do we need a setting that only allows the Kreditz-section for certain providers?
                requestObject.RemoveJsonProperty("Meta", true);
                requestObject.Add("Meta", JObject.FromObject(new
                {
                    ProviderName = CurrentProviderName,
                    CustomerExternalIpAddress = customerExternalIpAddress
                }));

                return ForwardApiRequest("nPreCredit", "api/UnsecuredLoanStandard/Create-Application", requestObject);
            });
        }
    }
}