using NTech.Services.Infrastructure;
using System;
using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class CreditReportsDwUpdateController : NController
    {
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                updateUrl = Url.Action("TriggerUpdate")
            });
            return View();
        }

        [HttpPost]
        [NTechApi]
        public ActionResult TriggerUpdate()
        {
            NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nCreditReport"]), NHttp.GetCurrentAccessToken())
                .PostJson("Api/DataWarehouse/Update", new { })
                .EnsureSuccessStatusCode();

            return Json(new { });
        }
    }
}