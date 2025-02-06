using System.Linq;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [RoutePrefix("Ui/MortgageLoans")]
    public class MortgageLoansController : NController
    {
        [Route("CreateLoan")]
        public ActionResult CreateLoan()
        {
            var providers = NEnv.GetAffiliateModels();
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                defaultProviderName = providers.Where(x => x.IsSelf == true).FirstOrDefault().ProviderName,
                providerNames = providers.Select(x => x.ProviderName).ToList(),
                clientCountry = NEnv.ClientCfg.Country.BaseCountry,
                clientName = NEnv.ClientCfg.ClientName
            }); ;
            return View();
        }

        [Route("CreateApplication")]
        public ActionResult CreateApplication()
        {
            var providers = NEnv.GetAffiliateModels();
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                baseCountry = NEnv.ClientCfg.Country.BaseCountry,
                defaultProviderName = providers.FirstOrDefault(x => x.IsSelf == true)?.ProviderName,
                providerNames = providers.Select(x => x.ProviderName).ToList(),
                clientName = NEnv.ClientCfg.ClientName,
                providers = NEnv.GetAffiliateModels()
            });
            return View();
        }
    }
}