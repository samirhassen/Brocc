using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;

namespace TestsnPreCredit
{
    [TestClass]
    public class HandlerLimitTests
    {
        private static int LEVEL_ZERO = 0;
        private static int LEVEL_ONE = 1;
        private static int LEVEL_TWO = 2;

        private HandlerLimitEngine CreateEngine() => new HandlerLimitEngine(
            new StrictMock<IPreCreditContextFactoryService>().Object,
            new StrictMock<IClientConfigurationCore>().Object);

        [TestMethod]
        public void ForLevelZero_TheLowestPossibleAmount_IsBelowLimit()
        {
            var e = CreateEngine();
            Assert.IsFalse(e.IsAllowed(LEVEL_ZERO, 1, 0));
        }

        [TestMethod]
        public void WithSeveralLevels_TheLevelLimits_AreRespected()
        {
            var e = CreateEngine();

            e.AddHandlerLimits(1000);

            Assert.IsFalse(e.IsAllowed(LEVEL_ZERO, 1000, 0));

            Assert.IsTrue(e.IsAllowed(LEVEL_ONE, 1000, 0));
            Assert.IsFalse(e.IsAllowed(LEVEL_ONE, 1001, 0));

            Assert.IsTrue(e.IsAllowed(LEVEL_TWO, 1001, 0));
        }
    }
}