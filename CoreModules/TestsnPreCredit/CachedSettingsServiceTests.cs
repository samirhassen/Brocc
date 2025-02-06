using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;

namespace TestsnPreCredit
{
    [TestClass]
    public class CachedSettingsServiceTests
    {
        private static DateTimeOffset BaseTime = new DateTimeOffset(2022, 10, 10, 0, 0, 0, 0, TimeSpan.Zero);

        [TestMethod]
        public void SettingsCacheRespectedCacheTimeAndChangeSignal()
        {
            var customerClient = new StrictMock<ICustomerClient>();
            var returnValue = "1";
            var settingName = "a";
            customerClient
                .Setup(x => x.LoadSettings("test"))
                .Returns(() => new Dictionary<string, string> { { settingName, returnValue } });

            var s = new CachedSettingsService(customerClient.Object);
            s.getUtcNow = () => BaseTime;

            //Initial value
            Assert.AreEqual("1", s.LoadSettings("test")[settingName]);

            //Changed but cache still holds
            returnValue = "2";
            Assert.AreEqual("1", s.LoadSettings("test")[settingName]);

            //Cache time ran out so value updated
            s.getUtcNow = () => BaseTime.AddMinutes(6);
            Assert.AreEqual("2", s.LoadSettings("test")[settingName]);

            //Changed but cache still holds
            returnValue = "3";
            Assert.AreEqual("2", s.LoadSettings("test")[settingName]);

            //Change signal received so value updated
            CachedSettingsService.OnSettingChanged("test");
            Assert.AreEqual("3", s.LoadSettings("test")[settingName]);
        }
    }
}
