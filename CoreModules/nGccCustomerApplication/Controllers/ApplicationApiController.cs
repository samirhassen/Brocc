using nGccCustomerApplication.Code;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication.Controllers
{
    [RoutePrefix("api/application")]
    public class ApplicationApiController : NController
    {
        [HttpPost]
        [Route("create")]
        public ActionResult CreateApplication(PreCreditClient.CreditApplicationRequest request)
        {
            if (request != null)
                request.RequestIpAddress = this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;
            var result = BalanziaController.CreateApplication(request, this.Url);
            return Json2(result);
        }

        [HttpPost]
        [Route("fetch-config")]
        public ActionResult FetchConfig()
        {            
            return Json2(new
            {
                IsTest = !NEnv.IsProduction,
                IsLegalInterestCeilingEnabled = NEnv.LegalInterestCeilingPercent.HasValue
            });
        }
    }
}