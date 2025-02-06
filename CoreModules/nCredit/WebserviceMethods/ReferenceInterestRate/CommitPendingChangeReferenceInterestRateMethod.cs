using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods
{
    public class CommitPendingChangeReferenceInterestRateMethod : TypedWebserviceMethod<CommitPendingChangeReferenceInterestRateMethod.Request, CommitPendingChangeReferenceInterestRateMethod.Response>
    {
        public override string Path => "ReferenceInterestRate/CommitPendingChange";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = requestContext.Service().ReferenceInterestChange;

            var pendingChange = s.GetPendingReferenceInterestChange();

            if (pendingChange == null)
                return Error("No pending change", httpStatusCode: 400, errorCode: "noPendingChange");

            var currentUser = requestContext.CurrentUserMetadata();
            if (pendingChange.InitiatedByUserId == currentUser.UserId && !currentUser.IsSystemUser)
            {
                if (NEnv.IsProduction || !request.RequestOverrideDuality.GetValueOrDefault())
                {
                    return Error("The change cannot be commited by the same user that initiated it unless it's a system user", httpStatusCode: 400, errorCode: "dualityRequired");
                }
            }

            if (request.ExpectedNewInterestRatePercent.HasValue && request.ExpectedNewInterestRatePercent.Value != pendingChange.NewInterestRatePercent)
                return Error($"Expected value {request.ExpectedNewInterestRatePercent.Value} differs from pending change {pendingChange.NewInterestRatePercent}", httpStatusCode: 400, errorCode: "expectedDiffersFromPending");

            string failedMessage;
            int nrOfCreditsUpdated = 0;
            var isOk = requestContext.Service().ReferenceInterestChange.TryChangeReferenceInterest(pendingChange, requestContext.CurrentUserMetadata(), out failedMessage, observeNrOfCreditsUpdated: x => nrOfCreditsUpdated = x);

            if (!isOk)
                return Error(failedMessage, httpStatusCode: 400);

            return new Response
            {
                NrOfCreditsUpdated = nrOfCreditsUpdated
            };
        }

        public class Request
        {
            public decimal? ExpectedNewInterestRatePercent { get; set; }
            public bool? RequestOverrideDuality { get; set; }
        }

        public class Response
        {
            public int NrOfCreditsUpdated { get; set; }
        }
    }
}