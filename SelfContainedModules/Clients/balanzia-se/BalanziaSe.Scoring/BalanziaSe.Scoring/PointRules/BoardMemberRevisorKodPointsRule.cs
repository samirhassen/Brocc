using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class BoardMemberRevisorKodPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            var r = context.RequireString("creditReportStyrelseRevisorKod");
            if (r == "Auktoriserad revisor")
                return 8;
            else if (r == "Godkänd revisor")
                return 5;
            else
                return 1;
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
            return ToSet("creditReportStyrelseRevisorKod");
        }
    }
}
