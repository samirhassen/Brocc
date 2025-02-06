using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.Code.Services;

namespace TestsnPreCredit.Credit
{
    [TestClass]
    public class LegalInterstRateCeilingTests
    {
        private LegalInterestCeilingService serviceWithCeiling = LegalInterestCeilingService.Create(20m);
        private LegalInterestCeilingService serviceWithoutCeiling = LegalInterestCeilingService.Create(null);

        [TestMethod]
        public void CeilingWorks()
        {
            Assert.AreEqual(10m, serviceWithCeiling.GetConstrainedMarginInterestRate(0m, 10m));
            Assert.AreEqual(19m, serviceWithCeiling.GetConstrainedMarginInterestRate(1m, 19m));
            Assert.AreEqual(19m, serviceWithCeiling.GetConstrainedMarginInterestRate(1m, 19.01m));
            Assert.AreEqual(19.01m, serviceWithCeiling.GetConstrainedMarginInterestRate(0.9m, 19.01m));
            Assert.AreEqual(20.34m, serviceWithCeiling.GetConstrainedMarginInterestRate(-0.34m, 20.34m));
            Assert.AreEqual(20.34m, serviceWithCeiling.GetConstrainedMarginInterestRate(-0.34m, 21.34m));

            //Sanity fallback to ensure the total interest never turns negative.
            Assert.AreEqual(100m, serviceWithCeiling.GetConstrainedMarginInterestRate(-100m, 2m));

            Assert.AreEqual(100m, serviceWithoutCeiling.GetConstrainedMarginInterestRate(0m, 100m));
        }

        [TestMethod]
        public void NewCreditWithoutInterestCeiling()
        {
            var c = serviceWithoutCeiling.HandleMarginInterestRateChange(1m, null, null, 25m);
            Assert.AreEqual(25m, c.NewMarginInterestRate);
            Assert.IsNull(c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void NewAdditionalLoanWithoutInterestCeiling()
        {
            var c = serviceWithoutCeiling.HandleMarginInterestRateChange(1m, null, 25m, 26m);
            Assert.AreEqual(26m, c.NewMarginInterestRate);
            Assert.IsNull(c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void TurningOffCeiling()
        {
            //If the law gets rolled back, will the system recover ie have the requested rate start tracking the actual since we cant delete.
            var c = serviceWithoutCeiling.HandleMarginInterestRateChange(1m, 22m, 19m, 21m);
            Assert.AreEqual(21m, c.NewMarginInterestRate);
            Assert.AreEqual(21m, c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void NewCreditWithInterestCeiling1()
        {
            var c = serviceWithCeiling.HandleMarginInterestRateChange(1m, null, null, 25m);
            Assert.AreEqual(19m, c.NewMarginInterestRate);
            Assert.AreEqual(25m, c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void NewAdditionalLoantWithInterestCeiling1()
        {
            var c = serviceWithCeiling.HandleMarginInterestRateChange(1m, 25m, 19m, 21m);
            Assert.IsNull(c.NewMarginInterestRate);
            Assert.AreEqual(21m, c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void NewCreditWithInterestCeiling2()
        {
            var c = serviceWithCeiling.HandleMarginInterestRateChange(1m, null, null, 19m);
            Assert.AreEqual(19m, c.NewMarginInterestRate);
            Assert.IsNull(c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void NewAdditionalLoantWithInterestCeiling2()
        {
            var c = serviceWithCeiling.HandleMarginInterestRateChange(1m, null, 19m, 21m);
            Assert.IsNull(c.NewMarginInterestRate);
            Assert.AreEqual(21m, c.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void ReferenceInterestChangeAfterEnablingTheCeiling()
        {
            var c1 = serviceWithCeiling.HandleReferenceInterestRateChange(-0.34m, null, 21m);
            Assert.AreEqual(20.34m, c1.NewMarginInterestRate);
            Assert.AreEqual(21m, c1.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void ReferenceInterestChangeRecoveryAfterLowering()
        {
            //Reference goes from -0.34 to -5.00
            var c1 = serviceWithCeiling.HandleReferenceInterestRateChange(-5, 21m, 20.34m);
            Assert.AreEqual(21m, c1.NewMarginInterestRate);
            Assert.IsNull(c1.NewRequestedMarginInterestRate);
        }


        [TestMethod]
        public void CompoundTestCase()
        {
            var c = serviceWithCeiling.HandleMarginInterestRateChange(10m, null, null, 17.9m);
            Assert.AreEqual(10m, c.NewMarginInterestRate);
            Assert.AreEqual(17.9m, c.NewRequestedMarginInterestRate);

            var c2 = serviceWithCeiling.HandleReferenceInterestRateChange(9m, 17.9m, 10m);
            Assert.AreEqual(11m, c2.NewMarginInterestRate);
            Assert.IsNull(c2.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void TestNegativeMarginInterestRate_TotalPositive()
        {
            var result = serviceWithoutCeiling.HandleMarginInterestRateChange(10m, null, null, -3m);
            Assert.AreEqual(-3m, result.NewMarginInterestRate);
            Assert.AreEqual(null, result.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void TestNegativeMarginInterestRate_TotalNegative()
        {
            var result = serviceWithoutCeiling.HandleMarginInterestRateChange(10m, null, null, -11m);
            Assert.AreEqual(-10m, result.NewMarginInterestRate);
            Assert.AreEqual(-11m, result.NewRequestedMarginInterestRate);
        }

        [TestMethod]
        public void TestNegativeMarginInterestRate_TotalNegative_BackToPositive()
        {
            var result = serviceWithoutCeiling.HandleReferenceInterestRateChange(12m, -11m, -10m);
            Assert.AreEqual(-11, result.NewMarginInterestRate);
            Assert.AreEqual(null, result.NewRequestedMarginInterestRate);
        }
    }
}
