using nCredit.DbModel.Repository;
using NTech.Core.Credit.Shared.Repository;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCustomerCreditHistoryController : NController
    {
        private ActionResult Fetch(List<int> customerIds, List<string> creditNrs)
        {
            var s = Service;
            var repo = new CustomerCreditHistoryCoreRepository(s.ContextFactory, NEnv.NotificationProcessSettings, NEnv.EnvSettings);
            return Json2(new { credits = repo.GetCustomerCreditHistory(customerIds, creditNrs) });
        }

        [HttpPost]
        [Route("Api/CustomerCreditHistoryBatch")]
        public ActionResult CustomerCreditHistoryBatch(List<int> customerIds)
        {
            return Fetch(customerIds, null);
        }

        [HttpPost]
        [Route("Api/CustomerCreditHistoryByCreditNrs")]
        public ActionResult CustomerCreditHistoryByCreditNrs(List<string> creditNrs)
        {
            return Fetch(null, creditNrs);
        }
    }
}