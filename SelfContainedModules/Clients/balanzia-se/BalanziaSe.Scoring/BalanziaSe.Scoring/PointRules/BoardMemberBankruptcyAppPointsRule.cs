using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class BoardMemberBankruptcyAppPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("creditReportFinnsStyrelseKonkursansokningar") == "missing")
                return 0;

            return context.RequireBool("creditReportFinnsStyrelseKonkursansokningar", null)
                ? 1
                : 8;
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
            return ToSet("creditReportFinnsStyrelseKonkursansokningar");
        }
    }
}
