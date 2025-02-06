using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    public abstract class IntScorePointScoringRule : ScoringRuleBase
    {
        public void Score(ScoringDataModel input, IScoringContext scoreContext)
        {
            var c = new RuleContext(this, input, scoreContext);

            var points = ComputeScore(c);
            scoreContext.AddScorePoints(this.RuleName, points);
        }
        
        protected abstract int ComputeScore(RuleContext context);

        protected int MeanValueForApplicants(RuleContext context, Func<int, int> f)
        {
            var applicantNrs = context.RequireApplicantNrs().ToList();
            var sum = applicantNrs.Sum(f);
            return (int)Math.Round(((decimal)sum) / ((decimal)applicantNrs.Count));
        }
    }
}