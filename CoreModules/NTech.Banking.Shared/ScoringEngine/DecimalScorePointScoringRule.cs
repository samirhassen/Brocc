using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{

    public abstract class DecimalScorePointScoringRule : ScoringRuleBase
    {
        public void Score(ScoringDataModel input, IScoringContext scoreContext)
        {
            var c = new RuleContext(this, input, scoreContext);

            var points = ComputeScore(c);
            scoreContext.AddScorePoints(this.RuleName, points);
        }

        protected abstract decimal ComputeScore(RuleContext context);

        protected decimal MeanValueForApplicants(RuleContext context, Func<int, decimal> f)
        {
            var applicantNrs = context.RequireApplicantNrs().ToList();
            return applicantNrs.Sum(f) / ((decimal)applicantNrs.Count);
        }
    }

    public abstract class WeightedDecimalScorePointScoringRule : ScoringRuleBase
    {
        public void PreScore(ScoringDataModel input, IScoringContext scoreContext, Dictionary<string, Tuple<decimal, decimal>> tempWeightsAndPoints)
        {
            var c = new RuleContext(this, input, scoreContext);

            var p = ComputeUnweightedPoints(c);
            var w = ComputeWeight(c);

            tempWeightsAndPoints[this.RuleName] = Tuple.Create(w, p);            
        }

        protected abstract decimal ComputeUnweightedPoints(RuleContext context);
        protected abstract decimal ComputeWeight(RuleContext context);

        public static Dictionary<string, Tuple<decimal, decimal>> PreparePreScore()
        {
            return new Dictionary<string, Tuple<decimal, decimal>>();
        }

        public static void Score(Dictionary<string, Tuple<decimal, decimal>> tempWeightsAndPoints, IScoringContext scoreContext)
        {
            var totalWeight = tempWeightsAndPoints.Sum(x => x.Value.Item1);
            foreach(var i in tempWeightsAndPoints)
            {
                Func<decimal, decimal> debugRound = x => Math.Round(x, 5); //Just to not make it super long and hard to read
                var debugText = ToJson(new Dictionary<string, decimal>
                {
                    { "weight", debugRound(i.Value.Item1) },
                    { "unweightedPoints", debugRound(i.Value.Item2) },
                    { "totalWeight", debugRound(totalWeight) }
                });                    
                scoreContext.SetScorePoints(i.Key, totalWeight == 0m ? 0m : Math.Round(((i.Value.Item1 * i.Value.Item2) / totalWeight), 2), debugData: debugText);
            }
        }

        private static string ToJson(Dictionary<string, decimal> numbers)
        {
            //To avoid installning Newtonsoft.Json. If this becomes more complex just to that or inject a func<object, string> converter from outside
            return "{" + string.Join(", ",
            numbers.Select(x =>
            {
                return "\"" + x.Key + "\": " + x.Value.ToString(CultureInfo.InvariantCulture);
            })) + "}";
        }

        protected decimal Capped(decimal computedValue, decimal minAllowedValue, decimal maxAllowedValue)
        {
            return computedValue > maxAllowedValue ? maxAllowedValue : (computedValue < minAllowedValue ? minAllowedValue : computedValue);
        }
    }
}