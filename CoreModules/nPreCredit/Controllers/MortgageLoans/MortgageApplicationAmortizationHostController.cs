using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeMortgageLoanMiddle]
    [RoutePrefix("Ui/MortgageLoan/Amortization")]
    public class MortgageApplicationAmortizationHostController : NController
    {
        [Route("New")]
        public ActionResult New(string applicationNr)
        {
            var vb = new
            {
                isNew = true,
                applicationNr = applicationNr,
                translation = GetTranslations()
            };
            SetInitialData(vb);
            return View();
        }
    }
}