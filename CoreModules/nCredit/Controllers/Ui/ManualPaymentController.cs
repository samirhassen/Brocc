using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class ManualPaymentController : NController
    {
        [HttpGet]
        [Route("Ui/Payments/RegisterManual")]
        public ActionResult Index()
        {
            ViewBag.SupressTestFunctions = true;

            SetInitialData(new
            {
                isDualityRequired = NEnv.IsManualPaymentDualityRequired,
                registerManualPaymentUrl = Url.Action("Create", "ApiCreateManualPayment"),
                today = Clock.Today,
                userId = this.CurrentUserId,
                isTest = !NEnv.IsProduction
            });
            return View();
        }
    }
}