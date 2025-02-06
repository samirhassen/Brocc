using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class ExternalCreditReportCompanyTypeRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {

            return context.RequireString("creditReportBolagsform", null) == "aktiebolag" 
                ? RuleContext.MimumDemandsResultCode.Accepted
                : RuleContext.MimumDemandsResultCode.Rejected;
        }
        
        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportBolagsform");
        }
    }
}