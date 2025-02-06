using nCredit.Code;
using nCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods
{
    public class BeginChangeReferenceInterestRateMethod : TypedWebserviceMethod<BeginChangeReferenceInterestRateMethod.Request, PendingReferenceInterestChangeModelWithUser>
    {
        public override string Path => "ReferenceInterestRate/BeginChange";

        protected override PendingReferenceInterestChangeModelWithUser DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.NewInterestRatePercent);
            });

            var s = requestContext.Service();
            var r = s.ReferenceInterestChange.BeginChangeReferenceInterest(request.NewInterestRatePercent.Value, requestContext.CurrentUserMetadata());
            var rr = AutoMapperHelper.Map<PendingReferenceInterestChangeModel, PendingReferenceInterestChangeModelWithUser>(r);
            if (rr != null)
                rr.InitiatedByUserName = s.UserDisplayName.GetUserDisplayNameByUserId(r.InitiatedByUserId.ToString());

            return rr;
        }

        public class Request
        {
            public decimal? NewInterestRatePercent { get; set; }
        }
    }
}