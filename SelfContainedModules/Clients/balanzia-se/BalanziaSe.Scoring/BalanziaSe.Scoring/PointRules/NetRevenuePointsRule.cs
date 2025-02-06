using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class NetRevenuePointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportBokslutDatum") == "missing")
                return 5;

            if (context.RequireString("creditReportNettoOmsattning", null) == "missing")
                return 0;

            var revenue = (decimal)context.RequireInt("creditReportNettoOmsattning", null);

            return Capped(Math.Truncate(((revenue / 1000m) + 1000m) / 1000m), 1, 15);
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 1m;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportNettoOmsattning", "creditReportBokslutDatum");
        }
    }
}
