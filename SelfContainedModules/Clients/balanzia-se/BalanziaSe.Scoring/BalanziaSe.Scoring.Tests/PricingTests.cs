using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring.Tests
{
    [TestClass]
    public class PricingTests: ScoringRuleTestBase
    {
        [TestMethod]
        public void WithOffer1()
        {
            var p = new BalanziaSeCompanyLoanPricingModelRule();

            var c = new ScoringContext();

            c.ScorePointsByRuleNames["Foo"] = 8.3m;

            p.Score(new ScoringDataModel()
                .Set("applicationAmount", "120000", null)
                .Set("applicationRepaymentTimeInMonths", "23", null)
                .Set("creditReportRiskklassForetag", "4", null)
                .Set("currentReferenceInterestRatePercent", "3.3", null), c);

            Assert.AreEqual(1200, c.Offer?.InitialFeeAmount);
            Assert.AreEqual(120000, c.Offer?.LoanAmount);
            Assert.AreEqual(11.7m, c.Offer?.NominalInterestRatePercent);
            Assert.AreEqual(6035.60m, c.Offer?.AnnuityAmount);
            Assert.AreEqual(50, c.Offer?.MonthlyFeeAmount);
        }
    }
}
