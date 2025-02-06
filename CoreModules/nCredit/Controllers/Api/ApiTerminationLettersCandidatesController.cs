using NTech.Services.Infrastructure;
using System;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditMiddle(ValidateAccessToken = true)]
    public class ApiTerminationLettersCandidatesController : NController
    {
        [HttpPost]
        [Route("Api/Credit/TerminationLetterCandidates/GetPage")]
        public ActionResult GetPage(int pageSize, int pageNr = 0, string omniSearch = null)
        {
            var result = Service.TerminationLetterCandidateService.GetTerminationLetterCandidatesPage(pageSize, pageNr, omniSearch,
                x => Url.Action("Index", "Credit", new { creditNr = x }, Request.Url.Scheme));

            return Json2(result);
        }
    }
}