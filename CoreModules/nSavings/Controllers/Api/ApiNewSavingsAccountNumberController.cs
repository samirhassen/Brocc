using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiNewSavingsAccountNumberController : NController
    {
        [HttpPost]
        [Route("Api/NewSavingsAccountNumber")]
        public ActionResult NewSavingsAccountNumber()
        {
            return Json2(new { nr = CreateSavingsAccountBusinessEventManager.GenerateNewSavingsAccountNumber(Service.ContextFactory) });
        }
    }
}