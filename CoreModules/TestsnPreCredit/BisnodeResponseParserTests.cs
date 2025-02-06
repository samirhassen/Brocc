using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCreditReport.Code;
using nCreditReport.Code.BisnodeFi;
using System;
using System.Linq;
using System.Xml.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class BisnodeResponseParserTests
    {
        [TestMethod]
        public void HasBusinessConnectionIs_Base_True()
        {
            RunBusinessConnectionTest("true");
        }

        [TestMethod]
        public void HasBusinessConnectionIs_OnVastuuHenkilo_K_No_Rows_True()
        {
            RunBusinessConnectionTest("true", d =>
            {
                d.Descendants().Where(x => x.Name.LocalName == "OnVastuuHenkilo").Single().Value = "K";
                d.Descendants().Where(x => x.Name.LocalName == "VastuuHenkiloTieto").ToList().ForEach(x => x.Remove());
            });
        }

        [TestMethod]
        public void HasBusinessConnectionIs_OnVastuuHenkilo_E_But_With_Rows_True()
        {
            RunBusinessConnectionTest("true", d =>
            {
                d.Descendants().Where(x => x.Name.LocalName == "OnVastuuHenkilo").Single().Value = "E";
            });
        }

        [TestMethod]
        public void HasBusinessConnectionIs_OnVastuuHenkilo_E_But_Without_Rows_False()
        {
            RunBusinessConnectionTest("false", d =>
            {
                d.Descendants().Where(x => x.Name.LocalName == "OnVastuuHenkilo").Single().Value = "E";
                d.Descendants().Where(x => x.Name.LocalName == "VastuuHenkiloTieto").ToList().ForEach(x => x.Remove());
            });
        }

        [TestMethod]
        public void PosteRestanteAddress()
        {
            AssertPropertyIncluded("hasPosteRestanteAddress", "false",
                x => x.Descendants("PostiOsoite").Descendants("PostiosoiteS").Single().Value = "Gatan 1");

            AssertPropertyIncluded("hasPosteRestanteAddress", "true",
                x => x.Descendants("PostiOsoite").Descendants("PostiosoiteS").Single().Value = "Poste restante");

            AssertPropertyIncluded("hasPosteRestanteAddress", "true",
                x =>
                {
                    x.Descendants("PostiOsoite").Descendants("PostiosoiteS").Single().Value = "Gatan 1";
                    x.Descendants("VakinainenOsoite").Descendants("LahiosoiteS").Single().Value = "Poste restante";
                });
        }

        [TestMethod]
        public void PostBoxAddress()
        {
            AssertPropertyIncluded("hasPostBoxAddress", "false",
                x => x.Descendants("VakinainenOsoite").Descendants("Postinumero").Single().Value = "01234");

            AssertPropertyIncluded("hasPostBoxAddress", "true",
                x => x.Descendants("VakinainenOsoite").Descendants("Postinumero").Single().Value = "01231");
        }

        [TestMethod]
        public void ImmigrationDate()
        {
            AssertPropertyIncluded("immigrationDate", "2004-08-27", x => x.Descendants("SuomeenMuuttopvm").Single().Value = "20040827");
        }

        #region "BaseXml"

        private const string BaseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SoliditetHenkiloLuottoTiedotResponse xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <KayttajaTiedot>
    <KayttajaTunnus>100653ML</KayttajaTunnus>
    <AsiakasTunnus>100653</AsiakasTunnus>
    <LoppuAsiakas_Nimi>balanzia</LoppuAsiakas_Nimi>
    <Versio>1</Versio>
  </KayttajaTiedot>
  <VastausLoki>
    <KysyttyHenkiloTunnus>010586-298F</KysyttyHenkiloTunnus>
    <SyyKoodi>1</SyyKoodi>
    <PaluuKoodi Koodi=""1"">Palveluvastaus onnistui</PaluuKoodi>
  </VastausLoki>
  <HenkiloTiedotResponse>
    <HenkiloTiedot>
      <HenkiloTunnus>010586-298F</HenkiloTunnus>
      <Hakutiedot>
        <Sanomatunnus>VRKD</Sanomatunnus>
        <OnnistuikoHaku Koodi=""0000"">Haku onnistui</OnnistuikoHaku>
      </Hakutiedot>
      <Henkilo>
        <NykyinenSukunimi>Valo</NykyinenSukunimi>
        <NykyisetEtunimet>Fredrik</NykyisetEtunimet>
        <VakinainenOsoite>
          <LahiosoiteS>Lukiokatu 23287</LahiosoiteS>
          <LahiosoiteR>Lukiokatu 23287</LahiosoiteR>
          <Postinumero>02880</Postinumero>
          <PostitoimipaikkaS>VEIKKOLA</PostitoimipaikkaS>
          <PostitoimipaikkaR>VEIKKOLA</PostitoimipaikkaR>
          <Valtiokoodi>246</Valtiokoodi>
          <AsuminenAlkupvm>20150225</AsuminenAlkupvm>
        </VakinainenOsoite>
        <EntisetKotimaisetOsoitteet>
          <EntinenVakinainenKotimainenLahiosoite>
            <LahiosoiteS>Vägen 3b</LahiosoiteS>
            <LahiosoiteR>Vägen 3b</LahiosoiteR>
            <Postinumero>02770</Postinumero>
            <PostitoimipaikkaS>ESPOO</PostitoimipaikkaS>
            <PostitoimipaikkaR>ESBO</PostitoimipaikkaR>
            <AsuminenAlkupvm>20130101</AsuminenAlkupvm>
            <AsuminenLoppupvm>20150224</AsuminenLoppupvm>
          </EntinenVakinainenKotimainenLahiosoite>
          <EntinenVakinainenKotimainenLahiosoite>
            <LahiosoiteS>Vägen 11 A</LahiosoiteS>
            <LahiosoiteR>Vägen 11 A</LahiosoiteR>
            <Postinumero>02180</Postinumero>
            <PostitoimipaikkaS>ESPOO</PostitoimipaikkaS>
            <PostitoimipaikkaR>ESBO</PostitoimipaikkaR>
            <AsuminenAlkupvm>20120601</AsuminenAlkupvm>
            <AsuminenLoppupvm>20121231</AsuminenLoppupvm>
          </EntinenVakinainenKotimainenLahiosoite>
          <EntinenVakinainenKotimainenLahiosoite>
            <LahiosoiteS>Vägen 10</LahiosoiteS>
            <LahiosoiteR>Vägen 10</LahiosoiteR>
            <Postinumero>02600</Postinumero>
            <PostitoimipaikkaS>ESPOO</PostitoimipaikkaS>
            <PostitoimipaikkaR>ESBO</PostitoimipaikkaR>
            <AsuminenAlkupvm>20081212</AsuminenAlkupvm>
            <AsuminenLoppupvm>20120531</AsuminenLoppupvm>
          </EntinenVakinainenKotimainenLahiosoite>
        </EntisetKotimaisetOsoitteet>
        <EntisetUlkomaisetOsoitteet />
        <TilapainenOsoite>
          <LahiosoiteS>Vägen 12</LahiosoiteS>
          <LahiosoiteR>Vägen 12</LahiosoiteR>
          <Postinumero>07940</Postinumero>
          <PostitoimipaikkaS>LOVIISA</PostitoimipaikkaS>
          <PostitoimipaikkaR>LOVISA</PostitoimipaikkaR>
          <Valtiokoodi>246</Valtiokoodi>
          <AsuminenAlkupvm>19921231</AsuminenAlkupvm>
          <AsuminenLoppupvm>19921231</AsuminenLoppupvm>
        </TilapainenOsoite>
        <PostiOsoite>
            <PostiosoiteS>Temp</PostiosoiteS>
            <PostiosoiteR />
            <Postinumero>00530</Postinumero>
            <PostitoimipaikkaS>HELSINKI</PostitoimipaikkaS>
            <PostitoimipaikkaR>HELSINGFORS</PostitoimipaikkaR>
            <Valtiokoodi>246</Valtiokoodi>
            <PostiosoiteAlkupvm>20160229</PostiosoiteAlkupvm>
            <PostiosoiteLoppupvm />
        </PostiOsoite>
        <Kotikunta>
          <Kuntanumero>257</Kuntanumero>
          <KuntaS>Kirkkonummi</KuntaS>
          <KuntaR>Kyrkslätt</KuntaR>
          <KuntasuhdeAlkupvm>20150225</KuntasuhdeAlkupvm>
        </Kotikunta>
        <SuomeenMuuttopvm />
        <Edunvalvonta>E</Edunvalvonta>
        <Aidinkieli>
          <Kielikoodi>fi</Kielikoodi>
          <KieliS>suomi</KieliS>
          <KieliR>finska</KieliR>
        </Aidinkieli>
        <Siviilisaaty>
          <Siviilisaatykoodi>2</Siviilisaatykoodi>
          <SiviilisaatyS>Avioliitossa</SiviilisaatyS>
          <SiviilisaatyR>Gift</SiviilisaatyR>
        </Siviilisaaty>
      </Henkilo>
      <BRIC Koodi=""-1"" Selite=""Ei luokitusta"">
        <PoikkeusIlmoitus>Henkilölle ei löydy luokitusta</PoikkeusIlmoitus>
      </BRIC>
    </HenkiloTiedot>
  </HenkiloTiedotResponse>
  <LuottoTietoMerkinnatResponse>
    <LuottoTietoMerkinnat Rekisteri=""Soliditet"">
      <HenkiloTunnus>010586-298F</HenkiloTunnus>
      <LuottoLuokka>0</LuottoLuokka>
      <MerkintojenLkm>0</MerkintojenLkm>
    </LuottoTietoMerkinnat>
  </LuottoTietoMerkinnatResponse>
  <YritysYhteydetResponse>
    <YritysYhteysTiedot>
      <HenkiloTunnus>010586-298F</HenkiloTunnus>
      <Nimi>Valo Fredrik</Nimi>
      <OnVastuuHenkilo>K</OnVastuuHenkilo>
      <VastuuHenkiloTieto>
        <YritysNimi>Företag1 Ab</YritysNimi>
        <Aktiivisuus>Lakannut</Aktiivisuus>
        <YhtioMuoto>Osakeyhtiö</YhtioMuoto>
        <Rating>EI-RATING</Rating>
        <LimiittiSuositus>0</LimiittiSuositus>
        <MaksuTapa>Ei negatiivinen</MaksuTapa>
        <YTunnus>09730093</YTunnus>
        <Duns>54-045-6977</Duns>
        <Rooli />
      </VastuuHenkiloTieto>
      <VastuuHenkiloTieto>
        <YritysNimi>Företag2 Ab</YritysNimi>
        <Aktiivisuus>Aktiivinen</Aktiivisuus>
        <YhtioMuoto>Osakeyhtiö</YhtioMuoto>
        <Rating>A</Rating>
        <LimiittiSuositus>0</LimiittiSuositus>
        <MaksuTapa>Ei negatiivinen</MaksuTapa>
        <YTunnus>20807987</YTunnus>
        <Duns>54-012-0990</Duns>
        <Rooli>hallituksen jäsen</Rooli>
      </VastuuHenkiloTieto>
      <VastuuHenkiloTieto>
        <YritysNimi>Företag3 Ab</YritysNimi>
        <Aktiivisuus>Aktiivinen</Aktiivisuus>
        <YhtioMuoto>Osakeyhtiö</YhtioMuoto>
        <Rating>AA</Rating>
        <LimiittiSuositus>0</LimiittiSuositus>
        <MaksuTapa>Ei negatiivinen</MaksuTapa>
        <YTunnus>23898132</YTunnus>
        <Duns>53-980-9367</Duns>
        <Rooli>hallituksen varajäsen</Rooli>
      </VastuuHenkiloTieto>
      <VastuuHenkiloTieto>
        <YritysNimi>Företag4 Ab</YritysNimi>
        <Aktiivisuus>Lakannut</Aktiivisuus>
        <YhtioMuoto>Kommandiittiyhtiö</YhtioMuoto>
        <Rating>EI-RATING</Rating>
        <LimiittiSuositus>0</LimiittiSuositus>
        <MaksuTapa>Ei negatiivinen</MaksuTapa>
        <YTunnus>06012934</YTunnus>
        <Duns>45-973-1170</Duns>
        <Rooli>vastuunalainen yhtiömies</Rooli>
      </VastuuHenkiloTieto>
      <VastuuHenkiloTieto>
        <YritysNimi>Valo Fredrik</YritysNimi>
        <Aktiivisuus>Aktiivinen</Aktiivisuus>
        <YhtioMuoto>Toiminimi</YhtioMuoto>
        <Rating>A</Rating>
        <LimiittiSuositus>0</LimiittiSuositus>
        <MaksuTapa>Ei negatiivinen</MaksuTapa>
        <YTunnus>17792585</YTunnus>
        <Duns>36-995-5062</Duns>
        <Rooli>elinkeinonharjoittaja</Rooli>
      </VastuuHenkiloTieto>
    </YritysYhteysTiedot>
  </YritysYhteydetResponse>
</SoliditetHenkiloLuottoTiedotResponse>";

        #endregion "BaseXml"

        private void AssertPropertyIncluded(string name, string value, Action<XDocument> transform)
        {
            RunTest(r =>
            {
                Assert.IsFalse(r.IsError, "IsError");
                var p = r.SuccessItems.Where(x => x.Name == name);
                Assert.AreEqual(1, p.Count(), $"Excepted {name} to occur exactly once");
                Assert.AreEqual(value, p.Single().Value, $"Wrong value on {name}");
            }, transform);
        }

        private void RunTest(Action<BisnodeFiResponseParser.Result> assert, Action<XDocument> transform = null)
        {
            var s = XDocuments.Parse(BaseXml);
            transform?.Invoke(s);
            var r = XmlSerializationUtil.Deserialize<nCreditReport.SoliditetFiWs.SoliditetHenkiloLuottoTiedotResponse>(s);
            var p = new BisnodeFiResponseParser();
            var result = p.Parse(r, false);
            foreach (var rr in result.SuccessItems)
            {
                Console.WriteLine($"{rr.Name}={rr.Value}");
            }
            assert(result);
        }

        private void RunBusinessConnectionTest(string expectedValue, Action<XDocument> transform = null)
        {
            RunTest(
                result => Assert.AreEqual(expectedValue, result.SuccessItems.SingleOrDefault(x => x.Name == "hasBusinessConnection")?.Value),
                transform);
        }
    }
}