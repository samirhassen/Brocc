using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code.Scoring.BalanziaScoringRules
{
    public class CoApplicantRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RequireInt("nrOfApplicants", null) > 1
                ? RuleContext.MimumDemandsResultCode.Rejected :
                RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet("");
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfApplicants");
        }
    }
}