using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class SolidityPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if(context.RequireString("creditReportBokslutDatum") == "missing" || context.RequireString("creditReportSoliditetProcent") == "missing")
                return 5;

            return Capped(
                Math.Truncate(context.RequireDecimal("creditReportSoliditetProcent", null) / 4m), 
                1, 15);
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 1;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportSoliditetProcent", "creditReportBokslutDatum");
        }
    }
}
