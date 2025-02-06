using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    public abstract class ManualControlScoringRule : ScoringRuleBase
    {
        public void Score(ScoringDataModel input, IScoringContext scoreContext)
        {
            var c = new RuleContext(this, input, scoreContext);

            if (ForceManualControl(c))
                scoreContext.AddManualAttention(this.RuleName, null);
        }

        protected abstract bool ForceManualControl(RuleContext context);
    }
}