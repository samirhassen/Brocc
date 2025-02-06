using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ApplicationWaitingForAdditionalInformation")]
    public class ApplicationWaitingForAdditionalInformationController : NController
    {
        private readonly IApplicationWaitingForAdditionalInformationService applicationWaitingForAdditionalInformationService;

        public ApplicationWaitingForAdditionalInformationController(IApplicationWaitingForAdditionalInformationService applicationWaitingForAdditionalInformationService)
        {
            this.applicationWaitingForAdditionalInformationService = applicationWaitingForAdditionalInformationService;
        }

        [HttpPost]
        [Route("Set")]
        public ActionResult Set(string applicationNr, bool? isWaitingForAdditionalInformation)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");
            if (!isWaitingForAdditionalInformation.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing isWaitingForAdditionalInformation");

            return Json2(applicationWaitingForAdditionalInformationService.SetIsWaitingForAdditionalInformation(applicationNr, isWaitingForAdditionalInformation.Value));
        }
    }
}