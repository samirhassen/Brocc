using System.Web.Mvc;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiNewSavingsAccountNumberController : NController
    {
        [HttpPost]
        [Route("Api/NewSavingsAccountNumber")]
        public ActionResult NewSavingsAccountNumber()
        {
            return Json2(new
            {
                nr = CreateSavingsAccountBusinessEventManager.GenerateNewSavingsAccountNumber(Service.ContextFactory)
            });
        }
    }
}