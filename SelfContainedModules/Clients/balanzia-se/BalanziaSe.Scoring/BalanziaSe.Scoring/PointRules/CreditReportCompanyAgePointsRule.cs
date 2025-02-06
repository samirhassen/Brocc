using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class CreditReportCompanyAgePointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportForetagAlderIManader", null) == "missing")
                return 0;

            var months = (decimal)context.RequireInt("creditReportForetagAlderIManader", null);
            if (Math.Truncate(months / 12) > 10)
                return 15;
            else
                return Capped(Math.Truncate(months / 6), 0, 15);
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 2;
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
