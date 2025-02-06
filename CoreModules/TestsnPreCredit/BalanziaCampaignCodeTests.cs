using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code.Balanzia;

namespace TestsnPreCredit
{
    [TestClass]
    public class BalanziaCampaignCodeTests
    {
        [TestMethod]
        public void RemoveInitialFee()
        {
            var b = BalanziaCampaignCode.CreateWithAllRulesActive();
            Assert.IsTrue(b.IsValidCode("H00031"));
            Assert.IsTrue(b.IsCodeThatRemovesInitialFee("H00031"));

            Assert.IsTrue(b.IsValidCode("H00030"));
            Assert.IsFalse(b.IsCodeThatRemovesInitialFee("H00030"));
        }

        [TestMethod]
        public void ForceManualControl()
        {
            var b = BalanziaCampaignCode.CreateWithAllRulesActive();
            Assert.IsTrue(b.IsValidCode("F00039"));
            Assert.IsTrue(b.IsCodeThatForcesManualControl("F00039"));

            Assert.IsTrue(b.IsValidCode("F00030"));
            Assert.IsFalse(b.IsCodeThatForcesManualControl("F00030"));
        }
    }
}
