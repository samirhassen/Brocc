using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{

    public abstract class PricingModelScoringRule : ScoringRuleBase
    {
        public void Score(ScoringDataModel input, IScoringContext scoreContext)
        {
            var c = new RuleContext(this, input, scoreContext);            

            var result = ComputeRiskClassAndPossibleOffer(scoreContext.GetScorePoints(), c);

            scoreContext.SetRiskClass(result.Item1);
            if(result.Item2 == null)
            {
                scoreContext.AddRejection(RuleName);
            }
            else
            {
                scoreContext.SetOffer(result.Item2);
            }

            if (result.Item3)
                scoreContext.AddManualAttention(RuleName, true);
        }

        protected Tuple<string, ScoringProcess.OfferModel, bool> Reject(string riskClass, bool requiresManualAttention = false)
        {
            return Tuple.Create(riskClass, (ScoringProcess.OfferModel)null, requiresManualAttention);
        }

        protected Tuple<string, ScoringProcess.OfferModel, bool> AcceptWithOffer(string riskClass, ScoringProcess.OfferModel offer, bool requiresManualAttention = false)
        {
            return Tuple.Create(riskClass, offer, requiresManualAttention);
        }

        protected abstract Tuple<string, ScoringProcess.OfferModel, bool> ComputeRiskClassAndPossibleOffer(decimal scorePoints, RuleContext context);
    }
}