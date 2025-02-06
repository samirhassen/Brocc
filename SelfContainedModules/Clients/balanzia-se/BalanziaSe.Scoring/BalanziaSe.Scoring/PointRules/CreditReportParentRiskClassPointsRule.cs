using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class CreditReportParentRiskClassPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if(context.RequireInt("creditReportAntalModerbolag", null) == 0)
            {
                var r = context.RequireString("creditReportRiskklassForetag");
                return CreditReportRiskClassPointsRule.GetPointsFromRiskKlassForetag(r);
            }
            else
            {
                if (!int.TryParse(context.RequireString("creditReportModerbolagRiskklassForetag"), out var rc)) //missing or letter class
                    return 0;

                return (rc * 3) - 1;
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
            return ToSet("creditReportAntalModerbolag", "creditReportModerbolagRiskklassForetag", "creditReportRiskklassForetag");
        }
    }
}
