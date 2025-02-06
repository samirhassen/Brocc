using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class ChangeReferenceInterestRateController : NController
    {
        [HttpGet]
        [Route("Ui/ChangeReferenceInterestRate/List")]
        public ActionResult Index()
        {
            ViewBag.SupressTestFunctions = true;

            var screenDate = Clock.Today;
            using (var context = new CreditContext())
            {
                var referenceInterestRate = context
                    .SharedDatedValues
                    .Where(x => x.Name == SharedDatedValueCode.ReferenceInterestRate.ToString())
                    .OrderByDescending(x => x.TransactionDate)
                    .ThenByDescending(x => x.Timestamp)
                    .Select(x => (decimal?)x.Value)
                    .FirstOrDefault();

                var u = this.GetCurrentUserMetadata();
                SetInitialData(new
                {
                    currentReferenceInterestRate = referenceInterestRate ?? 0m,
                    now = Clock.Now,
                    currentUserId = u.UserId,
                    currentUserDisplayName = this.GetUserDisplayNameByUserId(u.UserId.ToString()),
                    isTest = !NEnv.IsProduction
                });
                return View();
            }
        }
    }
}