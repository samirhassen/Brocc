using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class LoanPurposePointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            var p = context.RequireString("applicationLoanPurposeCode");
            if (p == "Anställa personal")
                return 3;
            else if (p == "Finansiera skuld")
                return 12;
            else if (p == "Förvärv")
                return 7;
            else if (p == "Generell likviditet/kassaflöde")
                return 3;
            else if (p == "Hemsida/marknadsföring")
                return 3;
            else if (p == "Inköp av lager")
                return 12;
            else if (p == "Oväntade utgifter")
                return 3;
            else if (p == "Renovering")
                return 7;
            else if (p == "Säsongsinvestering")
                return 12;
            else //Annat, missing, ...
                return 3;
            throw new NotImplementedException();
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
            return ToSet("applicationLoanPurposeCode");
        }
    }
}
