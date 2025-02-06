using nCredit.Code;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Dynamic;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiReportsApplicationAnalysisController : NController
    {
        [Route("Api/Reports/ApplicationAnalysis")]
        [HttpGet()]
        public ActionResult Get(DateTime date)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();
            try
            {
                var dc = new DataWarehouseClient();
                var e = new ExpandoObject();
                e.SetValues(d => d["forDate"] = date);
                var result = dc.CreateReport("ApplicationAnalysis1", e, callTimeout: TimeSpan.FromHours(1));
                return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"ApplicationAnalysis-{date.ToString("yyyy-MM-dd")}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create application analysis report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}