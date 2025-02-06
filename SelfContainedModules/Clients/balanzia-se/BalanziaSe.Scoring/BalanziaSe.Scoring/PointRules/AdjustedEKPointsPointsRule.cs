using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.PointRules
{
    public class AdjustedEKPointsPointsRule : WeightedDecimalScorePointScoringRule
    {
        protected override decimal ComputeUnweightedPoints(RuleContext context)
        {
            var summaEgetKapital = GetWithDefault("creditReportSummaEgetKapital", 0m, context);
            var summaObeskattadeReserver = GetWithDefault("creditReportSummaObeskattadeReserver", 0m, context);
            var summaImmateriellaTillgangar = GetWithDefault("creditReportSummaImmateriellaTillgangar", 0m, context);

            var points = Math.Truncate((summaEgetKapital + (summaObeskattadeReserver * 0.78m) - summaImmateriellaTillgangar) / (1000m * 150m));

            return Capped(points, 1, 15);
        }

        private decimal GetWithDefault(string name, decimal defaultValue, RuleContext context)
        {
            if (context.RequireString(name, null) == "missing")
                return defaultValue;
            return context.RequireDecimal(name, null);
        }

        protected override decimal ComputeWeight(RuleContext context)
        {
            return 3m;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportSummaEgetKapital", "creditReportSummaObeskattadeReserver", "creditReportSummaImmateriellaTillgangar");
        }
    }
}
