using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class CashLiquidityPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if(context.RequireString("creditReportBokslutDatum") == "missing" || context.RequireString("creditReportKassalikviditetProcent") == "missing")
                return 5;

            return Capped(
                Math.Truncate(context.RequireDecimal("creditReportKassalikviditetProcent", null) / 10m), 
                0, 15); //0 not a typo. The original model is like this.
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
            return ToSet("creditReportKassalikviditetProcent", "creditReportBokslutDatum");
        }
    }
}
