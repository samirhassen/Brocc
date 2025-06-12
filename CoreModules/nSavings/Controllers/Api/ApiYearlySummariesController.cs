using System.Web.Mvc;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [RoutePrefix("Api/YearlySummaries")]
    public class ApiYearlySummariesController : NController
    {
        [HttpPost]
        [Route("AllYearsWithSummariesForAccount")]
        public ActionResult FetchAllYearsWithSummariesForAccount(string savingsAccountNr)
        {
            return Json2(Service.YearlySummary.GetAllYearsWithSummaries(savingsAccountNr));
        }

        [HttpGet]
        [Route("Pdf")]
        public ActionResult ShowPdf(string savingsAccountNr, int year, string fileDownloadName = null)
        {
            var s = Service.YearlySummary.CreateSummaryPdf(savingsAccountNr, year);
            if (s == null)
                return HttpNotFound();
            return new FileStreamResult(s, "application/pdf")
            {
                FileDownloadName = fileDownloadName
            };
        }
    }
}