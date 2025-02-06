using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ProviderInfo")]
    public class ProviderInfoController : NController
    {
        private readonly IProviderInfoService providerInfoService;

        public ProviderInfoController(IProviderInfoService providerInfoService)
        {
            this.providerInfoService = providerInfoService;
        }
        [HttpPost]
        [Route("FetchSingle")]
        public ActionResult FetchSingle(string providerName)
        {
            return Json2(providerInfoService.GetSingle(providerName));
        }
    }
}