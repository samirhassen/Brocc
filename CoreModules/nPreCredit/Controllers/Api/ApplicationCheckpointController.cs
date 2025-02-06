using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ApplicationCheckpoint")]
    public class ApplicationCheckpointController : NController
    {
        private readonly ApplicationCheckpointService checkPointService;

        public ApplicationCheckpointController(ApplicationCheckpointService checkPointService)
        {
            this.checkPointService = checkPointService;
        }

        [HttpPost]
        [Route("FetchAllForApplication")]
        public ActionResult FetchAllForApplication(string applicationNr)
        {
            return Json2(checkPointService.GetCheckpointsForApplication(applicationNr));
        }

        [HttpPost]
        [Route("FetchReasonText")]
        public ActionResult FetchReasonText(int checkpointId)
        {
            return Json2(checkPointService.GetReasonText(checkpointId));
        }
    }
}