using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BalanziaSe.Scoring
{
    public class PausedByPriorApplicationRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RequireInt("pausedDays", null) > 0 ? RuleContext.MimumDemandsResultCode.Rejected : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("pausedDays");
        }
    }
}