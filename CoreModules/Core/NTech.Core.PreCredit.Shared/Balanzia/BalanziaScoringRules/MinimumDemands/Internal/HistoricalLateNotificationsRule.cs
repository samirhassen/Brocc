using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code.Scoring.BalanziaScoringRules
{
    public class HistoricalLateNotificationsRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            const int MaxOverDueLastSixMonths = 30;

            return context.RejectIfForAllApplicants(applicantNr =>
            {
                var currentlyOverdueNrOfDays = context.RequireInt("currentlyOverdueNrOfDays", applicantNr);
                var maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths = context.RequireInt("maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths", applicantNr);
                return currentlyOverdueNrOfDays > MaxOverDueLastSixMonths || maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths > MaxOverDueLastSixMonths;
            });
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet("currentlyOverdueNrOfDays", "maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths");
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfApplicants");
        }
    }
}