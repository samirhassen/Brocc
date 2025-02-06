using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code.Scoring.BalanziaScoringRules
{
    public class HistoricalLateLoansRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            const int MaxDaysOverDue = 60;

            return context.RejectIfForAnyApplicant(applicantNr =>
            {
                var currentlyOverdueNrOfDays = context.RequireInt("currentlyOverdueNrOfDays", applicantNr);
                var maxNrOfDaysBetweenDueDateAndPaymentEver = context.RequireInt("maxNrOfDaysBetweenDueDateAndPaymentEver", applicantNr);
                return currentlyOverdueNrOfDays > MaxDaysOverDue || maxNrOfDaysBetweenDueDateAndPaymentEver > MaxDaysOverDue;
            });
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet("currentlyOverdueNrOfDays", "maxNrOfDaysBetweenDueDateAndPaymentEver");
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfApplicants");
        }
    }
}