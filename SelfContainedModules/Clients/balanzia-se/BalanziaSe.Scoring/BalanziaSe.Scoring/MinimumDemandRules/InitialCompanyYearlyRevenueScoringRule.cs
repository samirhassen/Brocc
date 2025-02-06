using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class InitialCompanyYearlyRevenueScoringRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            decimal companyYearlyRevenue;
            if (context.RequireString("applicationCompanyYearlyRevenue", null) == "missing")
                companyYearlyRevenue = 0;
            else
                companyYearlyRevenue = context.RequireInt("applicationCompanyYearlyRevenue", null);

            return companyYearlyRevenue < 100000 ? RuleContext.MimumDemandsResultCode.Rejected : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("applicationCompanyYearlyRevenue");
        }
    }
}