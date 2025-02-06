using NTech.Core.PreCredit.Shared;
using System.Linq;

namespace nPreCredit.Code.Agreements
{
    //TODO: Remove this
    public static class AdditionalLoanSupport
    {
        public static bool? HasAdditionalLoanOffer(string applicationNr, IPreCreditContextExtended context, out string notApplicableMessage)
        {
            var app = context
                .CreditApplicationHeadersQueryable
                .Where(x => x.ApplicationNr == applicationNr)
                .Select(x => new { x.CreditCheckStatus, x.CurrentCreditDecision })
                .Single();

            return HasAdditionalLoanOffer(applicationNr, app.CreditCheckStatus, app.CurrentCreditDecision, out notApplicableMessage);
        }

        public static bool? HasAdditionalLoanOffer(string applicationNr, string creditCheckStatus, CreditDecision currentCreditDecision, out string notApplicableMessage)
        {
            var acceptedDecision = currentCreditDecision as AcceptedCreditDecision;

            if (creditCheckStatus != "Accepted" || acceptedDecision == null)
            {
                notApplicableMessage = "Credit check is not approved";
                return null;
            }

            var decisionModel = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(acceptedDecision.AcceptedDecisionModel);

            if (decisionModel != null && (!decisionModel.amount.HasValue || string.IsNullOrWhiteSpace(decisionModel.creditNr)))
            {
                notApplicableMessage = "Application has no offer";
            }

            notApplicableMessage = null;

            return decisionModel != null;
        }

    }
}