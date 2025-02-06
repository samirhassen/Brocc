using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCreditReport.Code.UcSe;
using Newtonsoft.Json;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class UcBusinessReportParserTests
    {
        [TestMethod]
        public void BusinessReportAb()
        {
            var r = UcCreditReportParserTests.LoadEmbeddedReport("UcBusiness-Ab-1");

            var p = new UcBusinessSeResponseParser();
            var result = p.Parse(r, new OrganisationNumberParser("SE").Parse("5560756792"), () => new DateTime(2019, 5, 22));

            var itemsDict = result.SuccessItems.ToDictionary(x => x.Name, x => x.Value);

            Action<string, string> assert = (name, expectedValue) =>
                Assert.AreEqual(expectedValue, itemsDict.ContainsKey(name) ? itemsDict[name] : null, name);

            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

            assert("upplysningDatum", "2019-05-21");
            assert("riskklassForetag", "5");
            assert("riskprognosForetagProcent", "0.05");
            assert("antalModerbolag", "1");
            assert("moderbolagRiskklassForetag", "5");
            assert("branschRiskprognosForetagProcent", "0.2");
            assert("snikod", "68203");
            assert("bolagsform", "aktiebolag");
            assert("regDatumForetag", "2006-12-21");

            assert("antalStyrelseLedamotsManader", "143");
            assert("styrelseLedamotMaxMander", "82");

            assert("bokslutDatum", "2017-12-31");
            assert("nettoOmsattning", "36853000");
            assert("avkastningTotKapProcent", "7.69");
            assert("kassalikviditetProcent", "0.07");
            assert("soliditetProcent", "2.03");
            assert("summaEgetKapital", "4607000");
            assert("summaObeskattadeReserver", "missing");
            assert("summaImmateriellaTillgangar", "0");

            assert("bokslutDatumFg", "2016-12-31");
            assert("nettoOmsattningFg", "38034000");
            assert("avkastningTotKapProcentFg", "7.43");
            assert("kassalikviditetProcentFg", "0.16");
            assert("soliditetProcentFg", "3.98");
            assert("summaEgetKapitalFg", "9387000");
            assert("summaObeskattadeReserverFg", "missing");
            assert("summaImmateriellaTillgangarFg", "0");

            assert("finnsStyrelseKonkursengagemang", "false");
            assert("finnsStyrelseBetAnmarkningar", "false");
            assert("finnsStyrelseKonkursansokningar", "false");
            assert("styrelseRevisorKod", "Revisionsföretag");
            assert("antalAnmarkningar", "missing");

            assert("companyName", "Nya Levator Industrihus AB");
            assert("addressStreet", "Lerbäcken 21 Lgh 1509");
            assert("addressZipcode", "21457");
            assert("addressCity", "Malmö");
            assert("phone", null);
        }
    }
}
