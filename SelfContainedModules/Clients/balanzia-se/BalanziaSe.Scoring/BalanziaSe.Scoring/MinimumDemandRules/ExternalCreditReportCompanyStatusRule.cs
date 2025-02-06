using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class ExternalCreditReportCompanyStatusRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RequireString("creditReportBolagsstatus", null).Equals("Ok", System.StringComparison.OrdinalIgnoreCase)
                ? RuleContext.MimumDemandsResultCode.Accepted
                : RuleContext.MimumDemandsResultCode.Rejected;
        }
        
        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportBolagsstatus");
        }
    }
}