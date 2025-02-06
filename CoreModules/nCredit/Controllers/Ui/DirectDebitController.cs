using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers.Ui
{
    [NTechAuthorizeCreditMiddle]
    public class DirectDebitController : NController
    {
        [Route("Ui/Credit/DirectDebit")]
        public ActionResult CreditDirectDebit(string creditNr)
        {
            if (string.IsNullOrEmpty(creditNr))
                return HttpNotFound("Missing credit number.");

            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound("Credit direct debit not enabled.");

            SetInitialData(new
            {
                creditNr
            });

            return View();
        }
    }
}