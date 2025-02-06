using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class NetDebtEbitApproximationPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            if (context.RequireString("applicationCompanyYearlyResult") == "missing")
                return 1;
            if (context.RequireString("applicationCompanyCurrentDebtAmount") == "missing")
                return 1;

            var applicationCompanyYearlyResult = context.RequireDecimal("applicationCompanyYearlyResult", null);
            var applicationCompanyCurrentDebtAmount = context.RequireDecimal("applicationCompanyCurrentDebtAmount", null);

            var q = (int)Math.Ceiling(applicationCompanyCurrentDebtAmount / applicationCompanyYearlyResult);

            switch(q)
            {
                case 0: return 16; //This can probably not happen since ceil
                case 1: return 16;
                case 2: return 12;
                case 3: return 9;
                case 4: return 8;
                case 5: return 7;
                case 6: return 6;
                case 7: return 5;
                case 8: return 3;
                case 9: return 2;
                default: return 1;
            }
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 5;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("applicationCompanyYearlyResult", "applicationCompanyCurrentDebtAmount");
        }
    }
}
