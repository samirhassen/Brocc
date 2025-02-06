using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.ScoringEngine
{
    public abstract class PluginScoringProcess : ScoringProcess
    {
        private readonly List<WeightedDecimalScorePointScoringRule> scorePointScoringRules;

        public PluginScoringProcess(
            List<MinimumDemandPass> minimumDemandPasses,
            List<WeightedDecimalScorePointScoringRule> scorePointScoringRules,
            PricingModelScoringRule pricingModelScoringRule) : base(minimumDemandPasses, scorePointScoringRules, pricingModelScoringRule)
        {
            this.scorePointScoringRules = scorePointScoringRules;
        }

        /// <summary>
        /// If you want to use the scoring data model for display and want to ensure some properties are present even if the rules that use them are never used add them here.
        /// </summary>
        public virtual IScoringDataModelConsumer PrefetchedVariables
        {
            get
            {
                return null;
            }
        }

        private Func<ScoringDataModel, ScoringDataModel> GetDataFetcher<T>(string objectId, List<T> consumers, IPluginScoringProcessDataSource dataSource) where T : IScoringDataModelConsumer
        {
            var applicantItems = new HashSet<string>();
            var applicationItems = new HashSet<string>();

            foreach (var r in consumers)
            {
                applicantItems.UnionWith(r.RequiredApplicantItems);
                applicationItems.UnionWith(r.RequiredApplicationItems);
            }

            return m => dataSource.GetItems(objectId,
                    applicationItems.Except(m.ApplicationItems.Keys.ToHashSetShared()).ToHashSetShared(),
                    applicantItems.Except(m.ApplicantItems.SelectMany(x => x.Value.Select(y => y.Key))).ToHashSetShared());
        }

        /// <param name="objectId">Application nr or credit nr or similar. Used by the datasource to fetch data.</param>
        /// <param name="dataSource">Source of all scoring model data</param>
        /// <param name="initialApplicationItems">If you want to use the scoring data model for display and want to ensure some properties are present even if the rules that use them are never used add them here.</param>
        /// <param name="initialApplicantItems">If you want to use the scoring data model for display and want to ensure some properties are present even if the rules that use them are never used add them here.</param>
        /// <returns></returns>
        public PluginScoringProcessResult Score(string objectId, IPluginScoringProcessDataSource dataSource)
        {
            var d = new ScoringDataModel();

            var h = dataSource.GetItemsWithInternalHistory(objectId, new HashSet<string>(), new HashSet<string>());
            if (h.ScoringData != null)
                d.AddDataFromOtherModel(h.ScoringData);

            if (PrefetchedVariables != null)
            {
                var f = GetDataFetcher(objectId, new List<IScoringDataModelConsumer> { PrefetchedVariables }, dataSource);
                d = f(d);
            }

            var resultExtended = ScoreExtended(
                d,
                fetchAdditionalDataForMinimumDemandPass: minimumDemandPasses.ToDictionary(x => x.PassName, x => GetDataFetcher(objectId, x.Rules, dataSource)),
                fetchAdditionalDataForScorePointsStep: GetDataFetcher(objectId, scorePointScoringRules, dataSource),
                fetchAdditionalDataForPricingStep: GetDataFetcher(objectId, new List<PricingModelScoringRule> { pricingModelScoringRule }, dataSource));

            var result = resultExtended.Result;

            if (ForceManualControlOnInitialScoring.Equals(true))
                resultExtended.RejectedManualAttentionRuleNames.UnionWith(result.RejectionRuleNames);

            return new PluginScoringProcessResult
            {
                ManualAttentionRuleNames = result.WasAccepted ? resultExtended.AcceptedManualAttentionRuleNames : resultExtended.RejectedManualAttentionRuleNames,
                WasAccepted = result.WasAccepted,
                Offer = result.Offer,
                RejectionRuleNames = result.RejectionRuleNames,
                RiskClass = result.RiskClass,
                ScorePointsByRuleName = result.ScorePointsByRuleName,
                DebugDataByRuleNames = result.DebugDataByRuleNames,
                ScoringData = result.ScoringData,
                HistoricalApplications = h.HistoricalApplications,
                HistoricalCredits = h.HistoricalCredits
            };
        }

        public abstract string Name { get; }
        public bool ForceManualControlOnInitialScoring { get; set; }
    }

    public class PluginScoringProcessResult : ScoringProcess.Result
    {
        public List<HistoricalCredit> HistoricalCredits { get; set; }
        public List<HistoricalApplication> HistoricalApplications { get; set; }
    }
}