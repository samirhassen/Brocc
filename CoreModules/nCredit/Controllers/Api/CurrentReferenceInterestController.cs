using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class CurrentReferenceInterestController : NController
    {
        [HttpPost]
        [Route("Api/ReferenceInterest/GetCurrent")]
        public ActionResult GetCurrent()
        {
            using (var context = CreateCreditContext())
            {
                var model = new DomainModel.SharedDatedValueDomainModel(context);
                var referenceInterestRatePercent = model.GetReferenceInterestRatePercent(Clock.Today);
                return Json2(new { referenceInterestRatePercent = referenceInterestRatePercent });
            }
        }
    }
}