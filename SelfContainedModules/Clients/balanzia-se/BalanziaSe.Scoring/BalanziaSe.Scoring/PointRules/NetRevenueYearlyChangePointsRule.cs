using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class NetRevenueYearlyChangePointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportNettoOmsattningFg") == "missing")
                return 5;
            var previous = (decimal)context.RequireInt("creditReportNettoOmsattningFg", null);
            if (previous == 0)
                return 5;

            var current = (decimal)(context.RequireString("creditReportNettoOmsattning") == "missing" ? 0 : context.RequireInt("creditReportNettoOmsattning", null));
            var div = (current - previous) / previous;
            return Capped(Math.Truncate(div * 10 + 8), 1, 15);
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 0.54m;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportNettoOmsattning", "creditReportNettoOmsattningFg");
        }
    }
}
