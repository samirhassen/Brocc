using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class CreditReportRiskPercentPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportRiskprognosForetagProcent") == "missing")
                return 0;

            var p = context.RequireDecimal("creditReportRiskprognosForetagProcent", null);

            return Capped(
                Math.Truncate(8m / p),
                0, 15);
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 3;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportRiskprognosForetagProcent");
        }
    }
}
