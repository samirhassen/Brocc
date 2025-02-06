using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    public class ScoringProcess
    {
        protected readonly List<MinimumDemandPass> minimumDemandPasses;
        private readonly Action<ScoringDataModel, ScoringContext> calculateScoringPoints;
        protected readonly PricingModelScoringRule pricingModelScoringRule;

        public ScoringProcess(
            List<MinimumDemandPass> minimumDemandPasses,
            Action<ScoringDataModel, ScoringContext> calculateScoringPoints,
            PricingModelScoringRule pricingModelScoringRule)
        {
            this.minimumDemandPasses = minimumDemandPasses;
            this.calculateScoringPoints = calculateScoringPoints;
            this.pricingModelScoringRule = pricingModelScoringRule;
        }

        public ScoringProcess(
            List<MinimumDemandPass> minimumDemandPasses,
            List<IntScorePointScoringRule> scorePointScoringRules,
            PricingModelScoringRule pricingModelScoringRule) : this(
                minimumDemandPasses,
                (m, c) =>
                {
                    foreach (var pointRule in scorePointScoringRules)
                    {
                        pointRule.Score(m, c);
                    }
                },
                pricingModelScoringRule)
        {
        }

        public ScoringProcess(
            List<MinimumDemandPass> minimumDemandPasses,
            List<WeightedDecimalScorePointScoringRule> scorePointScoringRules,
            PricingModelScoringRule pricingModelScoringRule) : this(
                minimumDemandPasses,
                (m, c) =>
                {
                    /*
                     * This split up craziness is because one client had scoring rules where the weight had logic on them
                     */
                    var tmp = WeightedDecimalScorePointScoringRule.PreparePreScore();

                    foreach (var pointRule in scorePointScoringRules)
                    {
                        pointRule.PreScore(m, c, tmp);
                    }

                    WeightedDecimalScorePointScoringRule.Score(tmp, c);
                },
                pricingModelScoringRule)
        {
        }

        public Result Score(ScoringDataModel initialData,
            Dictionary<string, Func<ScoringDataModel, ScoringDataModel>> fetchAdditionalDataForMinimumDemandPass = null,
            Func<ScoringDataModel, ScoringDataModel> fetchAdditionalDataForScorePointsStep = null,
            Func<ScoringDataModel, ScoringDataModel> fetchAdditionalDataForPricingStep = null)
        {
            return ScoreExtended(initialData,
                fetchAdditionalDataForMinimumDemandPass: fetchAdditionalDataForMinimumDemandPass,
                fetchAdditionalDataForScorePointsStep: fetchAdditionalDataForScorePointsStep,
                fetchAdditionalDataForPricingStep: fetchAdditionalDataForPricingStep)?.Result;
        }

        public ResultWithDetails ScoreExtended(ScoringDataModel initialData,
            Dictionary<string, Func<ScoringDataModel, ScoringDataModel>> fetchAdditionalDataForMinimumDemandPass = null,
            Func<ScoringDataModel, ScoringDataModel> fetchAdditionalDataForScorePointsStep = null,
            Func<ScoringDataModel, ScoringDataModel> fetchAdditionalDataForPricingStep = null)
        {
            var resultExtended = new ResultWithDetails
            {
                Result = new Result(),
                AcceptedManualAttentionRuleNames = new HashSet<string>(),
                RejectedManualAttentionRuleNames = new HashSet<string>()
            };
            var result = resultExtended.Result;

            var m = initialData.Copy();

            result.ScoringData = m;

            var context = new ScoringContext();

            Action mergeManualAttentions = () =>
            {
                context.AcceptedManualAttentions.ToList().ForEach(x => resultExtended.AcceptedManualAttentionRuleNames.Add(x));
                context.RejectedManualAttentions.ToList().ForEach(x => resultExtended.RejectedManualAttentionRuleNames.Add(x));
                result.ManualAttentionRuleNames = resultExtended.AcceptedManualAttentionRuleNames.Union(resultExtended.RejectedManualAttentionRuleNames).ToHashSetShared();
            };

            //Minimum demands
            var isRejectedByMinimumDemand = false;
            foreach (var minimumDemandPass in minimumDemandPasses)
            {
                if (fetchAdditionalDataForMinimumDemandPass != null && minimumDemandPass.PassName != null && fetchAdditionalDataForMinimumDemandPass.ContainsKey(minimumDemandPass.PassName))
                    m.AddDataFromOtherModel(fetchAdditionalDataForMinimumDemandPass[minimumDemandPass.PassName](m));

                foreach (var rule in minimumDemandPass.Rules)
                {
                    rule.Score(m, context);
                }

                mergeManualAttentions();

                context.Rejections.ToList().ForEach(x => result.RejectionRuleNames.Add(x));

                if (result.RejectionRuleNames.Any())
                {
                    isRejectedByMinimumDemand = true;
                    break;
                }
            }

            if (!isRejectedByMinimumDemand)
            {
                //Point calculation
                if (fetchAdditionalDataForScorePointsStep != null)
                    m.AddDataFromOtherModel(fetchAdditionalDataForScorePointsStep(m));

                this.calculateScoringPoints(m, context);

                mergeManualAttentions();

                context.Rejections.ToList().ForEach(x => result.RejectionRuleNames.Add(x));

                if (!result.RejectionRuleNames.Any())
                {
                    //Points and pricing
                    Action<Dictionary<string, decimal>, string, decimal> addPoints = (d, n, p) =>
                    {
                        if (!d.ContainsKey(n)) { d[n] = 0; }
                        d[n] += p;
                    };

                    foreach (var p in context.ScorePointsByRuleNames)
                        addPoints(result.ScorePointsByRuleName, p.Key, p.Value);

                    if (fetchAdditionalDataForPricingStep != null)
                        m.AddDataFromOtherModel(fetchAdditionalDataForPricingStep(m));

                    pricingModelScoringRule.Score(m, context);

                    result.RiskClass = context.RiskClass;
                    mergeManualAttentions();

                    context.Rejections.ToList().ForEach(x => result.RejectionRuleNames.Add(x));

                    if (!result.RejectionRuleNames.Any())
                    {
                        //Offer
                        if (context.Offer == null)
                            throw new Exception("Offer expected");

                        result.WasAccepted = true;
                        result.Offer = context.Offer;
                    }
                }
            }

            if (context.DebugDataByRuleNames != null)
            {
                result.DebugDataByRuleNames = new Dictionary<string, string>();
                foreach (var d in context.DebugDataByRuleNames)
                    result.DebugDataByRuleNames[d.Key] = d.Value;
            }

            return resultExtended;
        }

        public class MinimumDemandPass
        {
            public string PassName { get; set; }
            public List<MinimumDemandScoringRule> Rules { get; set; }

            public static MinimumDemandPass Create(string passName, params MinimumDemandScoringRule[] rules)
            {
                return new MinimumDemandPass
                {
                    PassName = passName,
                    Rules = rules.Where(x => x != null).ToList()
                };
            }
        }

        public class Result
        {
            public bool WasAccepted { get; set; }
            public ScoringDataModel ScoringData { get; set; }
            public HashSet<string> RejectionRuleNames { get; set; } = new HashSet<string>();
            public HashSet<string> ManualAttentionRuleNames { get; set; } = new HashSet<string>();
            public Dictionary<string, decimal> ScorePointsByRuleName { get; set; } = new Dictionary<string, decimal>();
            public Dictionary<string, string> DebugDataByRuleNames { get; set; } = null;
            public string RiskClass { get; set; }
            public OfferModel Offer { get; set; }
        }

        public class ResultWithDetails
        {
            public Result Result { get; set; }
            public HashSet<string> AcceptedManualAttentionRuleNames { get; set; } = new HashSet<string>();
            public HashSet<string> RejectedManualAttentionRuleNames { get; set; } = new HashSet<string>();
        }

        public class OfferModel
        {
            public decimal LoanAmount { get; set; }
            public decimal? MonthlyAmortizationAmount { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal NominalInterestRatePercent { get; set; }
            public decimal MonthlyFeeAmount { get; set; }
            public decimal InitialFeeAmount { get; set; }
            public DateTime? BindingUntilDate { get; set; }
        }
    }
}