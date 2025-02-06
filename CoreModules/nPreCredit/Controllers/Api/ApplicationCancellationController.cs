using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ApplicationCancellation")]
    public class ApplicationCancellationController : NController
    {
        private readonly IApplicationCancellationService applicationCancellationService;

        public ApplicationCancellationController(IApplicationCancellationService applicationCancellationService)
        {
            this.applicationCancellationService = applicationCancellationService;
        }

        [HttpPost]
        [Route("Cancel")]
        public ActionResult Cancel(string applicationNr)
        {
            string failedMessage;
            if (applicationCancellationService.TryCancelApplication(applicationNr, out failedMessage))
                return Json2(new { });
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        [HttpPost]
        [Route("Reactivate")]
        public ActionResult Reactivate(string applicationNr)
        {
            string failedMessage;
            if (applicationCancellationService.TryReactivateApplication(applicationNr, out failedMessage))
                return Json2(new { });
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }
    }
}