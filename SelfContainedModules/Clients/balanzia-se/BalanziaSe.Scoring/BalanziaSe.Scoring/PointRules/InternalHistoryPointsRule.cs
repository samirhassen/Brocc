using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class InternalHistoryPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireInt("historicalDebtCollectionCount", null) == 0)
                return 0;
            if (context.RequireInt("nrOfActiveLoans", null) == 0)
                return 0;
            if (context.RequireInt("maxNrOfDaysBetweenDueDateAndPaymentEver", null) > 60)
                return 8;
            return 16; //Implict from above nrOfActiveLoans > 0 && maxNrOfDaysBetweenDueDateAndPaymentEver <= 60
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            var p = ComputeUnweightedPoints(context);
            return p == 0 ? 0 : 5;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfActiveLoans", "historicalDebtCollectionCount", "maxNrOfDaysBetweenDueDateAndPaymentEver");
        }
    }
}
