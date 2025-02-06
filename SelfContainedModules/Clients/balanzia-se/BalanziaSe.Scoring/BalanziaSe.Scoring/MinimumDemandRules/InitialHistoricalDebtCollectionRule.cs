using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class InitialHistoricalDebtCollectionRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RequireInt("historicalDebtCollectionCount", null) > 0 ? RuleContext.MimumDemandsResultCode.Rejected : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("historicalDebtCollectionCount");
        }
    }
}