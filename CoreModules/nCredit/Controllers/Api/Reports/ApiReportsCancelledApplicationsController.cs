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
    public class ApiReportsCancelledApplicationsController : NController
    {
        protected override bool IsEnabled => !NEnv.IsStandardUnsecuredLoansEnabled;

        [Route("Api/Reports/CancelledApplications1")]
        [HttpGet()]
        public ActionResult Get(DateTime date)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();
            try
            {
                var dc = new DataWarehouseClient();
                var e = new ExpandoObject();
                dynamic de = e;
                de.forDate = date;
                var result = dc.CreateReport("CancelledApplications1", e);
                return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"CancelledApplications-{date.ToString("yyyy-MM-dd")}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create application analysis report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}