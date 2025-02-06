using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    public abstract class MinimumDemandScoringRule : ScoringRuleBase
    {
        public void Score(ScoringDataModel input, IScoringContext scoreContext)
        {
            var c = new RuleContext(this, input, scoreContext);

            var result = CheckMinimumDemand(c);
            if(result == RuleContext.MimumDemandsResultCode.AcceptedWithManualAttention || result == RuleContext.MimumDemandsResultCode.RejectedWithManualAttention)
                scoreContext.AddManualAttention(this.RuleName, result == RuleContext.MimumDemandsResultCode.AcceptedWithManualAttention);

            if (result == RuleContext.MimumDemandsResultCode.Rejected || result == RuleContext.MimumDemandsResultCode.RejectedWithManualAttention)
                scoreContext.AddRejection(this.RuleName);
        }
        
        protected abstract RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context);
    }
}