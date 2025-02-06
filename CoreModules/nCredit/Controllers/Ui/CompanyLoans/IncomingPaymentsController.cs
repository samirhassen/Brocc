using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class ImportCompanyLoansController : NController
    {
        [HttpGet]
        [Route("Ui/CompanyLoans/ImportLoansFile")]
        public ActionResult ImportLoansFile()
        {
            if (!NEnv.IsCompanyLoansEnabled)
                return HttpNotFound();

            ViewBag.BaseCountry = NEnv.ClientCfg.Country.BaseCountry;
            SetInitialData(new
            {
            });
            return View();
        }
    }
}