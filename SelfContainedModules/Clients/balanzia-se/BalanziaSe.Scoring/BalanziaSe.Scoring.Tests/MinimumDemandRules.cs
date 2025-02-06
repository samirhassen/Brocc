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
    public class MinimumDemandRules : ScoringRuleTestBase
    {
        [TestMethod]
        public void ExternalBoardMembershipAgeRule()
        {
            var r = new ExternalBoardMembershipAgeRule();

            AssertRejectedOnExactly(r, 
                new ScoringDataModel().Set("creditReportStyrelseLedamotMaxMander", "missing", null),
                r.RuleName);

            AssertRejectedOnExactly(r,
                new ScoringDataModel().Set("creditReportStyrelseLedamotMaxMander", "6", null),
                r.RuleName);

            AssertRejectedOnExactly(r,
                new ScoringDataModel().Set("creditReportStyrelseLedamotMaxMander", "7", null));
        }

        [TestMethod]
        public void ExternalKFMRiskRule()
        {
            var r = new ExternalKFMRiskRule();

            AssertRejectedOnExactlyWithManualAttention(r,
                new ScoringDataModel()
                .Set("creditReportAntalAnmarkningar", "1", null)
                .Set("creditReportRiskklassForetag", "2", null),
                new HashSet<string> { r.RuleName });
            AssertRejectedOnExactlyWithManualAttention(r,
                new ScoringDataModel()
                .Set("creditReportAntalAnmarkningar", "1", null)
                .Set("creditReportRiskklassForetag", "3", null),
                new HashSet<string> { r.RuleName });
            AssertRejectedOnExactly(r,
                new ScoringDataModel()
                .Set("creditReportAntalAnmarkningar", "1", null)
                .Set("creditReportRiskklassForetag", "4", null));
            AssertRejectedOnExactly(r,
                new ScoringDataModel()
                .Set("creditReportAntalAnmarkningar", "0", null)
                .Set("creditReportRiskklassForetag", "3", null));
            AssertRejectedOnExactly(r,
                new ScoringDataModel()
                .Set("creditReportAntalAnmarkningar", "missing", null)
                .Set("creditReportRiskklassForetag", "3", null));
        }

        [TestMethod]
        public void CashflowSensitivityRule()
        {
            var r = new CashflowSensitivityRule();

            var m = new ScoringDataModel()
                .Set("applicationAmount", 450000m, null)
                .Set("applicationRepaymentTimeInMonths", 12, null)
                .Set("nrOfMonthsLeftCurrentYear", 9, null)
                .Set("applicationCompanyYearlyRevenue", 3407000, null)
                .Set("applicationCompanyYearlyResult", 114100, null)
                .Set("applicationCompanyCurrentDebtAmount", 50000, null);

            AssertRejectedOnExactly(r, m, r.RuleName);

            Assert.AreEqual(-243842m, m.GetDecimalRequired("normalCashFlowEstimateAmount", null));
            Assert.AreEqual(-314822m, m.GetDecimalRequired("stressedCashFlowEstimateAmount", null));
        }

        [TestMethod]
        public void CashflowSensitivityRuleErrorCase()
        {
            var r = new CashflowSensitivityRule();

            var m = new ScoringDataModel()
                .Set("applicationAmount", 75000m, null)
                .Set("applicationRepaymentTimeInMonths", 25, null)
                .Set("nrOfMonthsLeftCurrentYear", 5, null)
                .Set("applicationCompanyYearlyRevenue", 800000m, null)
                .Set("applicationCompanyYearlyResult", 300000m, null)
                .Set("applicationCompanyCurrentDebtAmount", 19000m, null);

            AssertRejectedOnExactly(r, m);

            Assert.AreEqual(488605m, m.GetDecimalRequired("normalCashFlowEstimateAmount", null));
            Assert.AreEqual(288681m, m.GetDecimalRequired("stressedCashFlowEstimateAmount", null));
        }

        [TestMethod]
        public void ActivePendingApplication()
        {
            var r = new ActiveApplicationRule();

            var m = new ScoringDataModel()
                .Set("activeApplicationCount", 1, null)
                .Set("minActiveApplicationAgeInDays", 3, null);

            AssertRejectedOnExactly(r, new ScoringDataModel()
                .Set("activeApplicationCount", 1, null)
                .Set("minActiveApplicationAgeInDays", 3, null), r.RuleName);

            AssertRejectedOnExactly(r, new ScoringDataModel()
                .Set("activeApplicationCount", 1, null)
                .Set("minActiveApplicationAgeInDays", 4, null), r.RuleName);

            AssertRejectedOnExactly(r, new ScoringDataModel()
                .Set("activeApplicationCount", 0, null));
        }
    }
}
