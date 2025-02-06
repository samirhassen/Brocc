using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechAuthorizeSavingsMiddle]
    public class ManualPaymentController : NController
    {
        [HttpGet]
        [Route("Ui/Payments/RegisterManual")]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                registerManualPaymentUrl = Url.Action("Create", "ApiCreateManualPayment"),
                today = Clock.Today,
                userId = this.CurrentUserId,
                isTest = !NEnv.IsProduction
            });
            return View();
        }
    }
}