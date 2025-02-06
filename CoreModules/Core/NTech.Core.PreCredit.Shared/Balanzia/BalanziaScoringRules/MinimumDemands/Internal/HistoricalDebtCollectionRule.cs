using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code.Scoring.BalanziaScoringRules
{
    public class HistoricalDebtCollectionRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RejectIfForAnyApplicant(applicantNr => context.RequireInt("historicalDebtCollectionCount", applicantNr) > 0);
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet("historicalDebtCollectionCount");
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfApplicants");
        }
    }
}