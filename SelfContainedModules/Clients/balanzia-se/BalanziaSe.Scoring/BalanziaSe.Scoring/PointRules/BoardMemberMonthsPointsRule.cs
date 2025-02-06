using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class BoardMemberMonthsPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportAntalStyrelseLedamotsManader") == "missing")
                return 0;

            var months = (decimal)context.RequireInt("creditReportAntalStyrelseLedamotsManader", null);

            if(Math.Truncate(months / 12m) > 6)
                return 15;

            return Math.Truncate(months / 6);
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
            return ToSet("creditReportAntalStyrelseLedamotsManader");
        }
    }
}
