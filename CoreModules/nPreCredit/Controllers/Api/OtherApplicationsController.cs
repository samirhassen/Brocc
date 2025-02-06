using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/OtherApplications")]
    public class OtherApplicationsController : NController
    {
        private readonly IOtherApplicationsService otherApplicationsService;

        public OtherApplicationsController(IOtherApplicationsService otherApplicationsService)
        {
            this.otherApplicationsService = otherApplicationsService;
        }

        [HttpPost]
        [Route("Fetch")]
        public ActionResult Fetch(string applicationNr)
        {
            return Json2(otherApplicationsService.Fetch(applicationNr));
        }

        [HttpPost]
        [Route("FetchByCustomerIds")]
        public ActionResult FetchByCustomerIds(int[] customerIds, string applicationNr, bool includeApplicationObjects)
        {
            return Json2(otherApplicationsService.FetchByCustomerIds(customerIds, applicationNr, includeApplicationObjects));
        }

    }
}