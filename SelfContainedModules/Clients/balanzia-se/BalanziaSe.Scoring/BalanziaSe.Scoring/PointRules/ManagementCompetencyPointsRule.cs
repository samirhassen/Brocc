using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class ManagementCompetencyPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportForetagAlderIManader", null) == "missing")
                return 0;

            var months = context.RequireInt("creditReportForetagAlderIManader", null);

            if (months < 12)
                return 0;
            else if (months < 12 * 5)
                return 5;
            else if (months < 12 * 10)
                return 8;
            else
                return 14;
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 4;
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
