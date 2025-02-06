using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BalanziaSe.Scoring.Tests
{
    public abstract class ScoringRuleTestBase
    {
        private string PrintStrings(IEnumerable<string> s)
        {
            return string.Join(",", s);
        }

        protected ScoringDataModel M()
        {
            return new ScoringDataModel();
        }

        protected void AssertRejectedOnExactly(MinimumDemandScoringRule r, ScoringDataModel m, params string[] ruleNames)
        {
            AssertRejectedOnExactlyWithManualAttention(r, m, null, ruleNames);
        }

        protected void AssertRejectedOnExactlyWithManualAttention(MinimumDemandScoringRule r, ScoringDataModel m, HashSet<string> manualAttentions, params string[] ruleNames)
        {
            var context = new ScoringContext();

            manualAttentions = manualAttentions ?? new HashSet<string>();

            r.Score(m, context);

            if (context.DebugDataByRuleNames != null && context.DebugDataByRuleNames.ContainsKey(r.RuleName))
                Console.WriteLine(context.DebugDataByRuleNames[r.RuleName]);

            CollectionAssert.AreEqual(manualAttentions.ToList(), context.ManualAttentions.ToList(), $"Manual attention. Actual=[{PrintStrings(context.ManualAttentions)}]");
            Assert.AreEqual(0, context.ScorePointsByRuleNames.Count, $"Score points. Actual=[{PrintStrings(context.ScorePointsByRuleNames.Select(x => $"{x.Key}={x.Value}"))}]");
            CollectionAssert.AreEqual(new HashSet<string>(ruleNames).ToList(), context.Rejections.ToList(), "Rejections");
        }

        protected void AssertManualAttention(ManualControlScoringRule r, ScoringDataModel m, params string[] manualAttentionsI)
        {
            var context = new ScoringContext();

            var manualAttentions = (manualAttentionsI?.ToHashSet()) ?? new HashSet<string>();

            r.Score(m, context);

            CollectionAssert.AreEqual(manualAttentions.ToList(), context.ManualAttentions.ToList(), $"Manual attention. Actual=[{PrintStrings(context.ManualAttentions)}]");
            Assert.AreEqual(0, context.ScorePointsByRuleNames.Count, $"Score points. Actual=[{PrintStrings(context.ScorePointsByRuleNames.Select(x => $"{x.Key}={x.Value}"))}]");
            Assert.AreEqual(0, context.Rejections.Count, "Rejections");
        }

        protected void AssertScorePoints(WeightedDecimalScorePointScoringRule r, ScoringDataModel m, decimal weight, decimal points)
        {
            var context = new ScoringContext();

            var d = WeightedDecimalScorePointScoringRule.PreparePreScore();
            r.PreScore(m, context, d);           

            CollectionAssert.AreEqual(new HashSet<string>().ToList(), context.ManualAttentions.ToList(), $"Manual attention. Actual=[{PrintStrings(context.ManualAttentions)}]");
            CollectionAssert.AreEqual(new HashSet<string>().ToList(), context.Rejections.ToList(), "Rejections");

            var scorePointsDesc = $"Score points. Actual=[{PrintStrings(d.Select(x => $"{x.Key}={x.Value.Item1} * {x.Value.Item2}"))}]."
                + Environment.NewLine + "--Model--" + Environment.NewLine
                + ScoringModelToString(m);

            Assert.AreEqual(1, d.Count, scorePointsDesc);
            var k = d.Single();
            Assert.AreEqual(weight, k.Value.Item1, scorePointsDesc);
            Assert.AreEqual(points, k.Value.Item2, scorePointsDesc);
            Assert.AreEqual(r.RuleName, k.Key, scorePointsDesc);
        }

        private string ScoringModelToString(ScoringDataModel d)
        {
            var b = new StringBuilder();
            foreach (var a in d.ApplicantItems)
                foreach (var k in a.Value)
                    b.AppendLine($"applicant{a.Key}.{k.Key} = {k.Value}");

            foreach (var k in d.ApplicationItems)
                b.AppendLine($"application.{k.Key} = {k.Value}");

            return b.ToString();
        }

        protected decimal Kilo(decimal d)
        {
            return d * 1000;
        }
    }
}
