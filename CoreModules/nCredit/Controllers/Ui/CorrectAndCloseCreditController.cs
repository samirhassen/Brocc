using nCredit.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class CorrectAndCloseCreditController : NController
    {
        [HttpGet]
        [Route("Ui/CorrectAndCloseCredit")]
        public ActionResult Index()
        {
            SetInitialData(new
            {
            });
            return View();
        }

        [HttpPost]
        [NTechApi]
        [Route("Api/CorrectAndCloseCredit/Calculate")]
        public ActionResult Calculate(string creditNr)
        {
            
            var mgr = new CreditCorrectAndCloseBusinessEventManager(GetCurrentUserMetadata(), Service.ContextFactory,
                CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings, Service.CustomerRelationsMerge, Service.PaymentOrder);
            decimal writtenOffCapitalAmount;
            decimal writtenOffNonCapitalAmount;
            string failedMessage;
            var isOk = mgr.TryCorrectAndCloseCredit(creditNr, true, out writtenOffCapitalAmount, out writtenOffNonCapitalAmount, out failedMessage);

            return Json2(new
            {
                isOk = isOk,
                failedMessage = failedMessage,
                suggestion = isOk ? new
                {
                    creditNr = creditNr,
                    capitalDebtAmount = writtenOffCapitalAmount,
                    nonCapitalDebtAmount = writtenOffNonCapitalAmount
                } : null
            });
        }

        [HttpPost]
        [NTechApi]
        [Route("Api/CorrectAndCloseCredit/CorrectAndClose")]
        public ActionResult CorrectAndClose(string creditNr)
        {
            var mgr = new CreditCorrectAndCloseBusinessEventManager(GetCurrentUserMetadata(), Service.ContextFactory,
                CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings, Service.CustomerRelationsMerge, Service.PaymentOrder);
            decimal _; decimal __;
            string failedMessage;
            var isOk = mgr.TryCorrectAndCloseCredit(creditNr, false, out _, out __, out failedMessage);
            return Json2(new
            {
                isOk = isOk,
                failedMessage = failedMessage,
                creditNr = creditNr
            });
        }
    }
}