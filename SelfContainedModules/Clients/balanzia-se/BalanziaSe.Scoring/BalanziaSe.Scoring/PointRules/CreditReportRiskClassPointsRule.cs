using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class CreditReportRiskClassPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            var r = context.RequireString("creditReportRiskklassForetag");
            return GetPointsFromRiskKlassForetag(r);
        }

        public static int GetPointsFromRiskKlassForetag(string f)
        {
            if (!int.TryParse(f, out var r))
                return 0;

            switch (r)
            {
                case 1: return 2;
                case 2: return 5;
                case 3: return 8;
                case 4: return 11;
                case 5: return 14;
                default: return 2;
            }
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
            return ToSet("creditReportRiskklassForetag");
        }
    }
}
