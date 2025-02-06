using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    public class ScoringContext : IScoringContext
    {
        public HashSet<string> Rejections { get; set; } = new HashSet<string>();
        public HashSet<string> AcceptedManualAttentions { get; set; } = new HashSet<string>();
        public HashSet<string> RejectedManualAttentions { get; set; } = new HashSet<string>();

        public HashSet<string> ManualAttentions
        {
            get
            {
                return AcceptedManualAttentions?.Union(RejectedManualAttentions)?.ToHashSetShared();
            }
        }

        public Dictionary<string, decimal> ScorePointsByRuleNames { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, string> DebugDataByRuleNames { get; set; } = null;
        public string RiskClass { get; set; }
        public ScoringProcess.OfferModel Offer { get; set; }

        public IScoringContext AddRejection(string name)
        {
            Rejections.Add(name); return this;
        }

        public IScoringContext AddManualAttention(string name, bool? wasAccepted)
        {
            if (!wasAccepted.HasValue || wasAccepted.Value)
                AcceptedManualAttentions.Add(name);
            if (!wasAccepted.HasValue || !wasAccepted.Value)
                RejectedManualAttentions.Add(name);

            return this;
        }

        public IScoringContext AddScorePoints(string name, decimal points)
        {
            if (!ScorePointsByRuleNames.ContainsKey(name))
            {
                ScorePointsByRuleNames[name] = 0m;
            }
            ScorePointsByRuleNames[name] += points;
            return this;
        }

        public IScoringContext SetScorePoints(string name, decimal points, string debugData = null)
        {
            ScorePointsByRuleNames[name] = points;
            if (debugData != null)
                return SetDebugData(name, debugData);
            else
                return this;
        }

        public IScoringContext SetDebugData(string name, string debugData)
        {
            if (DebugDataByRuleNames == null)
                DebugDataByRuleNames = new Dictionary<string, string>();
            DebugDataByRuleNames[name] = debugData;
            return this;
        }

        public decimal GetScorePoints()
        {
            return ScorePointsByRuleNames.Sum(x => x.Value);
        }

        public IScoringContext SetRiskClass(string riskClass)
        {
            RiskClass = riskClass;
            return this;
        }

        public IScoringContext SetOffer(ScoringProcess.OfferModel offer)
        {
            Offer = offer;
            return this;
        }
    }

    public interface IScoringContext
    {
        IScoringContext AddRejection(string name);

        IScoringContext AddManualAttention(string name, bool? wasAccepted);

        IScoringContext AddScorePoints(string name, decimal points);

        IScoringContext SetScorePoints(string name, decimal points, string debugData = null);

        decimal GetScorePoints();

        IScoringContext SetRiskClass(string riskClass);

        IScoringContext SetOffer(ScoringProcess.OfferModel offer);

        IScoringContext SetDebugData(string name, string debugData);
    }
}