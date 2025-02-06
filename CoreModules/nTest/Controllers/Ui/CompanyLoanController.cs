using System;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [RoutePrefix("Ui/CompanyLoan")]
    public class CompanyLoanController : NController
    {
        [Route("CreateApplication")]
        public ActionResult CreateApplication()
        {
            var applicationUrlPattern = new Uri(new Uri(NEnv.ServiceRegistry.External["nPreCredit"]), "Ui/CompanyLoan/Application?applicationNr=NNNNN");
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                baseCountry = NEnv.ClientCfg.Country.BaseCountry,
                defaultProviderName = NEnv.DefaultProviderName,
                applicationUrlPrefix = applicationUrlPattern.ToString().Replace("NNNNN", "")
            });
            return View();
        }

        [Route("CreateLoan")]
        public ActionResult CreateLoan()
        {
            var loanUrlPattern = new Uri(new Uri(NEnv.ServiceRegistry.External["nCredit"]), "Ui/Credit?creditNr=NNNNN");
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                baseCountry = NEnv.ClientCfg.Country.BaseCountry,
                defaultProviderName = NEnv.DefaultProviderName,
                loanUrlPrefix = loanUrlPattern.ToString().Replace("NNNNN", "")
            });
            return View();
        }
    }
}