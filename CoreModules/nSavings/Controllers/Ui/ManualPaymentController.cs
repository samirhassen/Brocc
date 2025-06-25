using System.Web.Mvc;
using nSavings.Code;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui
{
    [NTechAuthorizeSavingsMiddle]
    public class ManualPaymentController : NController
    {
        [HttpGet]
        [Route("Ui/Payments/RegisterManual")]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = EncodeInitialData(new
            {
                registerManualPaymentUrl = Url.Action("Create", "ApiCreateManualPayment"),
                today = Clock.Today,
                userId = CurrentUserId,
                isTest = !NEnv.IsProduction
            });
            return View();
        }
    }
}