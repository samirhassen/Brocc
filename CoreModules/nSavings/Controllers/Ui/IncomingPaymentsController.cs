using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechAuthorizeSavingsMiddle]
    public class IncomingPaymentsController : NController
    {
        [HttpGet]
        [Route("Ui/IncomingPayments/ImportFile")]
        public ActionResult ImportFile()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                getFileDataUrl = Url.Action("GetFileData", "ApiIncomingPayments"),
                importFileUrl = Url.Action("ImportFile", "ApiIncomingPayments")
            });
            return View();
        }
    }
}