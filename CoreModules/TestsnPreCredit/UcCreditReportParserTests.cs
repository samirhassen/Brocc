using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCreditReport.Code.UcSe;
using NTech.Banking.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class UcCreditReportParserTests
    {
        //Cache these since making one takes ~300ms and the rest of the test takes ~5ms
        private static ThreadLocal<Func<XDocument, nCreditReport.UcSeService2.ucReply>> Deserializer = new ThreadLocal<Func<XDocument, nCreditReport.UcSeService2.ucReply>>(
            () => nCreditReport.Code.XmlSerializationUtil.CreateDeserializer<nCreditReport.UcSeService2.ucReply>());

        public static nCreditReport.UcSeService2.ucReply LoadEmbeddedReport(string filenamePrefix)
        {
            var document = EmbeddedResources.WithEmbeddedStream("NTech.UnitTests.Resources", $"{filenamePrefix}.zip", s =>
            {
                //Note that these are all testpersons
                using (var zf = new ZipFile(s))
                using (var i = zf.GetInputStream(zf.GetEntry($"{filenamePrefix}.xml")))
                {
                    return XDocuments.Load(i);
                }
            });
            return Deserializer.Value(document);
        }

        private Dictionary<string, string> ParseSuccess(string filenamePrefix)
        {
            var report = LoadEmbeddedReport(filenamePrefix);
            var p = new UcSeResponseParser();
            var result = p.Parse(report);

            Assert.AreEqual(false, result.IsError, result.ErrorMessage);

            foreach (var i in result.SuccessItems)
            {
                Console.WriteLine($"{i.Name}={i.Value}");
            }

            return result.SuccessItems.ToDictionary(x => x.Name, x => x.Value);
        }

        [TestMethod]
        public void Mikro()
        {
            var items = ParseSuccess("UcReport-Mikro");
            Func<string, string> opt = x => items.ContainsKey(x) ? items[x] : null;

            var expected = new Dictionary<string, string>
            {
                { "templateName", "90" }, //3 om fullständing, 90 för mikro

                { "firstName", "Erik Magnus" },
                { "lastName", "Arnoldsson" },

                { "addressCountry", "SE" },
                { "hasDomesticAddress", "true" },
                { "domesticAddressSinceDate", null }, //Saknas va?
                { "addressStreet", "Holavedsplan 29 Lgh 1610" },
                { "addressZipcode", "36075" },
                { "addressCity", "Alstermo" },
                { "hasRegisteredMunicipality", "false" }, //Ej med i mikro
                { "registeredMunicipality", null },
                { "hasAddressChange", "false" },
                { "addressChangeDate", null },

                { "personstatus", "hasguardian" },
                { "nrOfPaymentRemarks", "7" },
                { "hasPaymentRemark", "true" },

                { "templateAccepted", "false" },
                { "templateManualAttention", "false" },
                { "templateReasonCode", "FRVSKSASKAN2" },
                { "riskPercent", null },
                { "riskValue", null },
                { "latestIncomePerYear", null },
                { "latestIncomeYear", null },
                { "hasGuardian", "true" }
            };

            foreach (var kvp in expected)
                Assert.AreEqual(kvp.Value, opt(kvp.Key));
        }

        [TestMethod]
        public void Full()
        {
            var items = ParseSuccess("UcReport-Full");
            Func<string, string> opt = x => items.ContainsKey(x) ? items[x] : null;

            var expected = new Dictionary<string, string>
            {
                { "templateName", "3" }, //3 om fullständing, 90 för mikro

                { "firstName", "Kjell-Åke Lennart" },
                { "lastName", "Svensson" },

                { "addressCountry", "SE" },
                { "hasDomesticAddress", "true" },
                { "domesticAddressSinceDate", null }, //Saknas va?
                { "addressStreet", "Malmplan 6" },
                { "addressZipcode", "69791" },
                { "addressCity", "Sköllersta" },
                { "hasRegisteredMunicipality", "true" },
                { "registeredMunicipality", "Askersund" },
                { "hasAddressChange", "false" },
                { "addressChangeDate", null },

                { "personstatus", "deactivated" },
                { "nrOfPaymentRemarks", "0" },
                { "hasPaymentRemark", "false" },

                { "templateAccepted", "true" },
                { "templateManualAttention", "false" },
                { "templateReasonCode", null },
                { "riskPercent", "99.4" },
                { "riskValue", "99.4" },
                { "latestIncomePerYear", "7200" },
                { "latestIncomeYear", "2017" },
                { "hasGuardian", "false" }
            };

            foreach (var kvp in expected)
                Assert.AreEqual(kvp.Value, opt(kvp.Key), kvp.Key);
        }

        [TestMethod]
        public void HistoricalAddressChanges()
        {
            var items = ParseSuccess("UcReport-Historical-AddressChanges");
            Func<string, string> opt = x => items.ContainsKey(x) ? items[x] : null;

            var expected = new Dictionary<string, string>
            {
                { "hasAddressChange", "true" },
                { "addressChangeDate", "2018-08-01" }
            };

            foreach (var kvp in expected)
                Assert.AreEqual(kvp.Value, opt(kvp.Key));
        }
    }
}