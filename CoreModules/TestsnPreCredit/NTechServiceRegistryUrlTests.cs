using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace TestsnPreCredit
{
    [TestClass]
    public class NTechServiceRegistryUrlTests
    {
        [TestMethod]
        public void PathsCombineCorrectly()
        {
            Func<string, string, string> create = (x, y) => NTechServiceRegistry.CreateUrl(new Uri(x), y).ToString();

            Assert.AreEqual("http://localhost:19727/Api/LoggedRequest/lendo/bank_api/json?a=1", create("http://localhost:19727/Api/LoggedRequest/lendo/?a=1", "bank_api/json"));
            Assert.AreEqual("http://localhost:19727/Api/LoggedRequest/lendo/bank_api/json?a=2", create("http://localhost:19727/Api/LoggedRequest/lendo?a=2", "bank_api/json"));
            Assert.AreEqual("http://localhost:19727/Api/LoggedRequest/lendo/bank_api/json?a=3", create("http://localhost:19727/Api/LoggedRequest/lendo?a=3", "/bank_api/json"));
            Assert.AreEqual("http://localhost:19727/Api/LoggedRequest/lendo/bank_api/json?a=4", create("http://localhost:19727/Api/LoggedRequest/lendo?a=4", "/bank_api/json/"));

            Assert.AreEqual("http://localhost:2635/id/connect/token", create("http://localhost:2635", "id/connect/token"));
            Assert.AreEqual("http://localhost:2635/id/connect/token", create("http://localhost:2635/", "id/connect/token"));
            Assert.AreEqual("http://localhost/nUser/id/connect/token", create("http://localhost/nUser", "id/connect/token"));
            Assert.AreEqual("http://localhost:4242/nUser/id/connect/token", create("http://localhost:4242/nUser", "id/connect/token"));
            Assert.AreEqual("https://example.org/id/connect/token", create("https://example.org", "id/connect/token"));
            Assert.AreEqual("https://example.org/id/connect/token", create("https://example.org/", "id/connect/token"));
            Assert.AreEqual("https://example.org/test/id/connect/token", create("https://example.org/test", "id/connect/token"));
            Assert.AreEqual("https://example.org/test/id/connect/token", create("https://example.org/test/", "id/connect/token"));

            Assert.AreEqual(
                "http://localhost:19727/Api/LoggedRequest/lendo/bank_api/json?a=4&c=x+y+z",
                NTechServiceRegistry.CreateUrl(new Uri("http://localhost:19727/Api/LoggedRequest/lendo?a=4"), "/bank_api/json/", Tuple.Create("c", "x y z")).ToString());

            Assert.AreEqual(
                "http://localhost:3412/Archive/Fetch?key=85105e20-1148-4b10-8c57-81fd17ac7824.pdf",
                create("http://localhost:3412/", "Archive/Fetch?key=85105e20-1148-4b10-8c57-81fd17ac7824.pdf"));

            Assert.AreEqual(
                "http://localhost:3412/Archive/Fetch?rootKey=42&key=85105e20-1148-4b10-8c57-81fd17ac7824.pdf",
                create("http://localhost:3412/?rootKey=42", "Archive/Fetch?key=85105e20-1148-4b10-8c57-81fd17ac7824.pdf"));
        }

        [TestMethod]
        public void TestHashBangUrl()
        {
            /*
                          this.apiClient.getUserModuleUrl('nCustomerPages', 'a/#/eid-login', {
                    t: `q_${ai.ApplicationNr}`
             */
            var r = new NTechServiceRegistry(new Dictionary<string, string>
            {
                { "nCustomerPages", "https://customerpages.example.org" }
            }, new Dictionary<string, string>());

            var url1 = r.External.ServiceUrl("nCustomerPages", "a/#/eid-login", Tuple.Create("t", "q_A123"));
            Assert.AreEqual("https://customerpages.example.org/a/#/eid-login?t=q_A123", url1.ToString());

            var url2 = r.External.ServiceUrl("nCustomerPages", "a/eid-login", Tuple.Create("t", "q_A123"));
            Assert.AreEqual("https://customerpages.example.org/a/eid-login?t=q_A123", url2.ToString());
        }
    }
}