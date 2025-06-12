using System.Web.Mvc;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui
{
    [NTechAuthorizeSavingsMiddle]
    public class IncomingPaymentsController : NController
    {
        [HttpGet]
        [Route("Ui/IncomingPayments/ImportFile")]
        public ActionResult ImportFile()
        {
            ViewBag.JsonInitialData = EncodeInitialData(new
            {
                getFileDataUrl = Url.Action("GetFileData", "ApiIncomingPayments"),
                importFileUrl = Url.Action("ImportFile", "ApiIncomingPayments")
            });
            return View();
        }
    }
}