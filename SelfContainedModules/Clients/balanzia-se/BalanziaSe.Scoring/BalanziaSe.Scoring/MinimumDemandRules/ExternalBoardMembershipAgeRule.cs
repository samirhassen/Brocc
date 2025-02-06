using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring
{
    public class ExternalBoardMembershipAgeRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            if (context.RequireString("creditReportStyrelseLedamotMaxMander") == "missing")
                return RuleContext.MimumDemandsResultCode.Rejected;

            return context.RequireInt("creditReportStyrelseLedamotMaxMander", null) <= 6 
                ? RuleContext.MimumDemandsResultCode.Rejected 
                : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportStyrelseLedamotMaxMander");
        }
    }
}
