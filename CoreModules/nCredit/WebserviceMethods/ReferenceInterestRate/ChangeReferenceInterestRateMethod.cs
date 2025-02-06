using NTech.Services.Infrastructure.NTechWs;
using Serilog;

namespace nCredit.WebserviceMethods
{

    public class ChangeReferenceInterestRateMethod : TypedWebserviceMethod<ChangeReferenceInterestRateMethod.Request, ChangeReferenceInterestRateMethod.Response>
    {
        public override string Path => "Credit/ChangeReferenceInterestRate";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.NewInterestRatePercent);
            });

            var u = requestContext.CurrentUserMetadata();
            if (!u.IsSystemUser)
                return Error("Only systemusers can bypass the duality requirement", httpStatusCode: 400, errorCode: "dualityRequired");

            var change = new Code.Services.PendingReferenceInterestChangeModel
            {
                NewInterestRatePercent = request.NewInterestRatePercent.Value,
                InitiatedByUserId = u.UserId,
                InitiatedDate = requestContext.Clock().Now.DateTime
            };
            string failMessage;
            int? nrOfCreditsUpdated = null;
            var s = requestContext.Service().ReferenceInterestChange;
            if (s.TryChangeReferenceInterest(change, u, out failMessage, observeNrOfCreditsUpdated: x => nrOfCreditsUpdated = x))
            {
                return new Response
                {
                    nrOfCreditsUpdated = nrOfCreditsUpdated ?? 0,
                    currentReferenceInterestRate = request.NewInterestRatePercent.Value
                };
            }
            else
            {
                NLog.Warning("Could not change reference interest rate {reason}", failMessage);
                return Error(failMessage, httpStatusCode: 400);
            }
        }

        public class Request
        {
            public decimal? NewInterestRatePercent { get; set; }
        }

        public class Response
        {
            public int nrOfCreditsUpdated { get; set; } //! lower case for legacy compatibility reasons
            public decimal currentReferenceInterestRate { get; set; }
        }
    }
    /*
        [HttpPost]
        [Route("Api/Credit/GetReferenceInterestRateChangesPage")]
        public ActionResult GetReferenceInterestRateChangesPage(int pageSize, int pageNr = 0)
        {
            return Json2(this.Service.ReferenceInterestChange.GetReferenceInterestRateChangesPage(pageSize, pageNr: pageNr));
        }     
     
     */
}