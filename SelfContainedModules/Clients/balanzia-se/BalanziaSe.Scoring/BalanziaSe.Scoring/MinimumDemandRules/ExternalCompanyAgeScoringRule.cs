using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class ExternalCompanyAgeScoringRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            int age;
            if (context.RequireString("creditReportForetagAlderIManader", null) == "missing")
                age = 0;
            else
                age = context.RequireInt("creditReportForetagAlderIManader", null);

            return age < 6 ? RuleContext.MimumDemandsResultCode.Rejected : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportForetagAlderIManader");
        }
    }
}