using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods
{

    public class CancelPendingChangeReferenceInterestRateMethod : TypedWebserviceMethod<CancelPendingChangeReferenceInterestRateMethod.Request, CancelPendingChangeReferenceInterestRateMethod.Response>
    {
        public override string Path => "ReferenceInterestRate/CancelPendingChange";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = requestContext.Service().ReferenceInterestChange;
            bool wasCancelled = false;
            s.CancelChangeReferenceInterest(observeWasAnyCancelled: x => wasCancelled = x);
            if (wasCancelled)
                return new Response { };
            else
                return Error("No pending change exists", httpStatusCode: 400, errorCode: "noPendingChange");
        }

        public class Request
        {

        }

        public class Response
        {

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