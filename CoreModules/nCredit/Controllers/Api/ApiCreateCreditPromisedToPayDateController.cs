using nCredit.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditMiddle(ValidateAccessToken = true)]
    public class ApiCreateCreditPromisedToPayDateController : NController
    {
        [HttpPost]
        [Route("Api/Credit/PromisedToPayDate/Add")]
        public ActionResult AddPromisedToPayDate(string creditNr, DateTime promisedToPayDate, bool? avoidReaddingSameValue)
        {
            using (var context = CreateCreditContext())
            {
                var mgr = new PromisedToPayBusinessEventManager(GetCurrentUserMetadata(), CoreClock.SharedInstance, NEnv.ClientCfgCore);
                mgr.TryAdd(creditNr, promisedToPayDate, avoidReaddingSameValue ?? true, context);

                context.SaveChanges();

                return Json2(new { });
            }
        }

        [HttpPost]
        [Route("Api/Credit/PromisedToPayDate/Remove")]
        public ActionResult RemovePromisedToPayDate(string creditNr)
        {
            using (var context = CreateCreditContext())
            {
                var mgr = new PromisedToPayBusinessEventManager(GetCurrentUserMetadata(), CoreClock.SharedInstance, NEnv.ClientCfgCore);
                mgr.TryRemove(creditNr, context);

                context.SaveChanges();

                return Json2(new { });
            }
        }
    }
}