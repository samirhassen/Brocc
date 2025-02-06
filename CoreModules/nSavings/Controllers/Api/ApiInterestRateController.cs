using nSavings.DbModel.BusinessEvents;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/InterestRate")]
    public class ApiInterestRateController : NController
    {
        [HttpPost]
        [Route("FetchCurrentByAccountTypeCode")]
        public ActionResult FetchCurrentInterestRate(string savingsAccountTypeCode)
        {
            var c = Enums.Parse<SavingsAccountTypeCode>(savingsAccountTypeCode);
            if (!c.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountTypeCode");

            using (var context = new SavingsContext())
            {
                var rate = ChangeInterestRateBusinessEventManager.GetCurrentInterestRateForNewAccounts(context, SavingsAccountTypeCode.StandardAccount, Clock.Today);

                return Json2(new
                {
                    HasRate = rate != null,
                    Rate = rate
                });
            }
        }
    }
}