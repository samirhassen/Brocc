using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiSavingsAccountInterestHistoryController : NController
    {

        [HttpPost]
        [Route("Api/SavingsAccount/InterestHistory")]
        public ActionResult InterestHistory(string savingsAccountNr)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

            using (var context = new DbModel.SavingsContext())
            {
                var a = ApiSavingsAccountDetailsController
                     .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                     .Where(x => x.SavingsAccountNr == savingsAccountNr)
                     .Select(x => new { x.Status, x.CreatedByBusinessEventId, x.CreatedTransactionDate, x.StatusBusinessEventId, x.AccountTypeCode })
                     .SingleOrDefault();

                if (a == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                var closedBusinessEventId = a.Status == SavingsAccountStatusCode.Closed.ToString() ? a.StatusBusinessEventId : null;

                var interestRates = ChangeInterestRateBusinessEventManager
                    .GetSavingsAccountFilteredActiveInterestRates(context, savingsAccountNr, a.CreatedTransactionDate, closedBusinessEventId)
                    .Select(x => new
                    {
                        x.Id,
                        x.TransactionDate,
                        x.ValidFromDate,
                        x.InterestRatePercent,
                    })
                    .ToList();

                return Json2(new
                {
                    interestRates = interestRates
                });
            }
        }
    }
}