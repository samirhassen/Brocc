using nCredit.Code;
using nCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods
{

    public class FetchPendingChangeReferenceInterestRateMethod : TypedWebserviceMethod<FetchPendingChangeReferenceInterestRateMethod.Request, PendingReferenceInterestChangeModelWithUser>
    {
        public override string Path => "ReferenceInterestRate/FetchPendingChange";

        protected override PendingReferenceInterestChangeModelWithUser DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = requestContext.Service();
            var r = s.ReferenceInterestChange.GetPendingReferenceInterestChange();
            var rr = AutoMapperHelper.Map<PendingReferenceInterestChangeModel, PendingReferenceInterestChangeModelWithUser>(r);
            if (rr != null)
                rr.InitiatedByUserName = s.UserDisplayName.GetUserDisplayNameByUserId(r.InitiatedByUserId.ToString());
            return rr;
        }

        public class Request
        {

        }
    }
}