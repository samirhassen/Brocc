using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public partial class ApiCreditDecisionDetailsController : NController
    {
        [Route("CreditDecisionDetails/FetchCreditReport")]
        [HttpPost]
        public ActionResult FetchCreditReport(int creditReportId, List<string> fieldNames)
        {
            var c = Service.Resolve<CreditReportService>();
            var creditReport = c.GetCreditReportById(creditReportId, fieldNames ?? new List<string>() { "*" });
            return Json2(new { creditReport = creditReport });
        }
    }
}