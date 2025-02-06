using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code.Services;
using nPreCredit.Code.StandardPolicyFilters.Rules;
using System.Collections.Generic;
using System.Globalization;

namespace TestsnPreCredit.StandardPolicyFilters
{
    [TestClass]
    public class EbaWeightedAveragePolicyFilterTests
    {
        private static ComplexApplicationList.Row AddLoanToSettle(bool? shouldBeSettled, int? amount, decimal? interestRatePercent, ComplexApplicationList loansToSettle)
        {
            var d = new Dictionary<string, string>();
            if (shouldBeSettled.HasValue) d["shouldBeSettled"] = shouldBeSettled.Value ? "true" : "false";
            if (amount.HasValue) d["currentDebtAmount"] = amount.ToString();
            if (interestRatePercent.HasValue) d["currentInterestRatePercent"] = interestRatePercent.Value.ToString(CultureInfo.InvariantCulture);
            return loansToSettle.AddRow(initialUniqueItems: d);
        }

        [TestMethod]
        public void SingleLoanWithValues()
        {
            var loansToSettle = ComplexApplicationList.CreateEmpty("LoansToSettle");
            AddLoanToSettle(true, 100, 10.59m, loansToSettle);

            var result = MinAllowedWeightedAverageSettlementInterestRateRule.ComputeWeightedSettlementInterestRateScoringVariables(loansToSettle);

            Assert.AreEqual(true, result.HasLoansToSettle);
            Assert.AreEqual(10.59m, result.MinSettlementInterestRatePercent);
            Assert.AreEqual(10.59m, result.WeightedAverageSettlementInterestRatePercent);
        }

        [TestMethod]
        public void MissingInterestIsTreatedAsZero()
        {
            var loansToSettle = ComplexApplicationList.CreateEmpty("LoansToSettle");
            AddLoanToSettle(true, 100, null, loansToSettle);

            var result = MinAllowedWeightedAverageSettlementInterestRateRule.ComputeWeightedSettlementInterestRateScoringVariables(loansToSettle);

            Assert.AreEqual(true, result.HasLoansToSettle);
            Assert.AreEqual(0m, result.MinSettlementInterestRatePercent);
            Assert.AreEqual(0m, result.WeightedAverageSettlementInterestRatePercent);
        }

        [TestMethod]
        public void LoansMissingAmountAreIgnored()
        {
            var loansToSettle = ComplexApplicationList.CreateEmpty("LoansToSettle");
            AddLoanToSettle(true, null, 10.59m, loansToSettle);

            var result = MinAllowedWeightedAverageSettlementInterestRateRule.ComputeWeightedSettlementInterestRateScoringVariables(loansToSettle);

            Assert.AreEqual(false, result.HasLoansToSettle);
        }

        [TestMethod]
        public void NonSettledLoansAreIgnored()
        {
            var loansToSettle = ComplexApplicationList.CreateEmpty("LoansToSettle");
            AddLoanToSettle(true, 100, 10m, loansToSettle);
            AddLoanToSettle(false, 1000, 15m, loansToSettle);

            var result = MinAllowedWeightedAverageSettlementInterestRateRule.ComputeWeightedSettlementInterestRateScoringVariables(loansToSettle);

            Assert.AreEqual(true, result.HasLoansToSettle);
            Assert.AreEqual(10m, result.MinSettlementInterestRatePercent);
            Assert.AreEqual(10m, result.WeightedAverageSettlementInterestRatePercent);
        }

        [TestMethod]
        public void WeightedAverageComputed()
        {
            var loansToSettle = ComplexApplicationList.CreateEmpty("LoansToSettle");
            AddLoanToSettle(true, 100, 10m, loansToSettle);
            AddLoanToSettle(true, 1000, 15m, loansToSettle);

            var result = MinAllowedWeightedAverageSettlementInterestRateRule.ComputeWeightedSettlementInterestRateScoringVariables(loansToSettle);

            Assert.AreEqual(true, result.HasLoansToSettle);
            Assert.AreEqual(10m, result.MinSettlementInterestRatePercent);
            Assert.AreEqual(14.55m, result.WeightedAverageSettlementInterestRatePercent);
        }
    }
}
