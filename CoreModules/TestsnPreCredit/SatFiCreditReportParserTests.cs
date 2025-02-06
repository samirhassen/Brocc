using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCreditReport.Code;
using nCreditReport.Code.SatFi;
using NTech.Banking.CivicRegNumbers;
using System;
using System.IO;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class SatFiCreditReportParserTests : SatFiCreditReportParser
    {
        private readonly string FileDownloadLocation = $@"C:\temp";
        private static string PersonIdPassive = "010100-126H";

        /*
         * Useful test cases
         * 
        private static string WithDateOfDeath = "010169-0938";
        private static string DeclaredLegallyIncompetent = "010177-878M";
        private static string AlsoLegallyIncompetent = "010275-067S";
        private static string SupervisionButNotRestricted = "010177-878M";
        */

        [Ignore("Runs with specified files, run manually when you want to test from file. ")]
        [TestMethod]
        public void TestParseInternalValuesFromFile_ForQType37()
        {
            var qType = "37";
            var document = File.ReadAllText($@"{FileDownloadLocation}\{PersonIdPassive}-{qType}.xml");

            var parser = new SatFiCreditReportParser();
            parser.Initiate(document, qType);
            var values = parser.ParseInternalValues();

            Assert.IsNotNull(values.Single(x => x.name == "addressCountry").value);
        }

        [Ignore("Runs with specified files, run manually when you want to test from file. ")]
        [TestMethod]
        public void TestParseInternalValuesFromFile_QType41()
        {
            var qType = "41";
            var document = File.ReadAllText($@"{FileDownloadLocation}\{PersonIdPassive}-{qType}.xml");

            var parser = new SatFiCreditReportParser();
            parser.Initiate(document, qType);
            var res = parser.ParseInternalValues();
        }

        [DataTestMethod]
        [DataRow("090776-0954", "1976-07-09")]
        [DataRow("090776+0954", "1876-07-09")]
        [DataRow("090701A0954", "2001-07-09")]
        [DataRow("090711A0954", "2011-07-09")]
        [DataRow("010275-0817", "1975-02-01")]
        [DataRow("121212-0817", "1912-12-12")]
        [DataRow("301201-0954", "1901-12-30")]
        public void SplitPersonId(string personId, string date)
        {
            // ddmmyyXnnnc
            var xml = $@"
            <populationInformation>
                <personData>
                    <personIdentificationData>
                        <personId>{personId}</personId>
                    </personIdentificationData>
                </personData>
            </populationInformation>";

            Initiate(xml, "37");
            var result = BirthDate();
            Assert.AreEqual(date, result);
        }

        [Ignore("Runs with specified files, run manually when you want to test from file. ")]
        [TestMethod]
        public void DownloadMultipleSATCreditResponses_ToLocalComputer()
        {
            var populateSatAccountInfoButDoNotCheckIn = new SatAccountInfo();
            var service = new SatFiCreditReportService(false, new DocumentClient(), populateSatAccountInfoButDoNotCheckIn);
            var p = new CivicRegNumberParser(service.ForCountry);

            void Download(string civicRegNr, string qType)
            {
                var uri = service.SetupRequestUrl(p.Parse(civicRegNr), qType);
                var res = service.GetResponseFromSat(uri, qType);

                File.WriteAllText($@"{FileDownloadLocation}\{civicRegNr}-{qType}.xml", res);
            }

            var civic = "010771-289X";
            Download(civic, "37");
            Download(civic, "41");
        }

        [Ignore("Runs with specified files, run manually when you want to test from file. ")]
        [TestMethod]
        public void Download_And_Parse_Tabled_Values_For_Testing()
        {
            var populateSatAccountInfoButDoNotCheckIn = new SatAccountInfo();
            var service = new SatFiCreditReportService(false, new DocumentClient(), populateSatAccountInfoButDoNotCheckIn);
            var p = new CivicRegNumberParser(service.ForCountry);

            void Download(string civicRegNr, string qType)
            {
                if (!File.Exists($@"{FileDownloadLocation}\{civicRegNr}-{qType}.xml"))
                {
                    var uri = service.SetupRequestUrl(p.Parse(civicRegNr), qType);
                    var res = service.GetResponseFromSat(uri, qType);
                    File.WriteAllText($@"{FileDownloadLocation}\{civicRegNr}-{qType}.xml", res);
                }
            }

            void GetParsed(string personId, string qt)
            {
                Console.WriteLine($"\t For qtype {qt}");
                var document = File.ReadAllText($@"{FileDownloadLocation}\{personId}-{qt}.xml");
                var parser = new SatFiCreditReportParser();
                parser.Initiate(document, qt);
                var values = parser.ParseTabledValues();
                values.ForEach(x => Console.WriteLine($"{x.Key} : {x.Value}"));
            }

            var civic = "010100-125F";
            Download(civic, "37");
            Download(civic, "41");

            GetParsed(civic, "37");
            Console.WriteLine();
            GetParsed(civic, "41");
        }

        [Ignore("Runs with specified files, run manually when you want to test from file. ")]
        [TestMethod]
        public void GetTabledValues_ForQtype37()
        {
            var qType = "37";
            var document = System.IO.File.ReadAllText($@"{FileDownloadLocation}\{PersonIdPassive}-{qType}.xml");

            var parser = new SatFiCreditReportParser();
            parser.Initiate(document, qType);
            var values = parser.ParseTabledValues();

            values.ForEach(x => Console.WriteLine($"{x.Key} : {x.Value}"));

            //Assert.AreEqual("", values.Single(x => x.Key == "Date of death").Value);
            //Assert.AreEqual("Finnish", values.Single(x => x.Key == "Native language").Value);
            //Assert.AreNotEqual("", values.Single(x => x.Key == "Permanent address in Finland").Value);
            //Assert.AreEqual("Finland", values.Single(x => x.Key == "Person's citizenship").Value);
        }

        [Ignore("Runs with specified files, run manually when you want to test from file. ")]
        [TestMethod]
        public void GetTabledValues_ForQtype41()
        {
            var qType = "41";
            var document = System.IO.File.ReadAllText($@"{FileDownloadLocation}\{PersonIdPassive}-{qType}.xml");

            var parser = new SatFiCreditReportParser();
            parser.Initiate(document, qType);
            var values = parser.ParseTabledValues();

            values.ForEach(x => Console.WriteLine($"{x.Key} : {x.Value}"));

            //Assert.AreNotEqual("", values.Single(x => x.Key == "Sole trader data (E)").Value);
        }

        [TestMethod]
        public void Citizenship_WillReturnOnlyLatestIfExists()
        {
            var expectedCountry = "Finland";
            var xml = $@"
            <populationInformation>
                <personData>               
                    <nationalityData>
                        <count>02</count>
                        <nationalityRow>
                            <code>246</code>
                            <text>{expectedCountry}</text>
                        </nationalityRow>
                        <nationalityRow>
                            <code>246</code>
                            <text>Sweden</text>
                        </nationalityRow>
                    </nationalityData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var personCitizenship = PersonsCitizenship();

            Assert.AreEqual(expectedCountry, personCitizenship);
        }

        [TestMethod]
        public void Citizenship_ReturnAllCitizenships()
        {
            var expected = "Finland, Sweden";
            var xml = $@"
            <populationInformation>
                <personData>               
                    <nationalityData>
                        <count>02</count>
                        <nationalityRow>
                            <code>246</code>
                            <text>Finland</text>
                        </nationalityRow>
                        <nationalityRow>
                            <code>246</code>
                            <text>Sweden</text>
                        </nationalityRow>
                    </nationalityData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = NationalitiesCommaSeparated();

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Citizenship_ReturnEmptyIfNotExistinting()
        {
            var xmlWithoutNationalityDataRows = $@"
            <populationInformation>
                <personData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xmlWithoutNationalityDataRows, qType);
            var personCitizenship = PersonsCitizenship();

            Assert.AreEqual("", personCitizenship);
        }

        [TestMethod]
        public void Supervision_WhenNoSupervisionAppointed()
        {
            var xmlWithutSupervision = $@"
            <populationInformation>
                <personData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xmlWithutSupervision, qType);
            var restriction = RestrictionOfCompetenceToAct();
            var startDate = SupervisionStartDate();

            Assert.AreEqual("No", restriction);
            Assert.AreEqual("", startDate);
        }

        [DataTestMethod]
        [DataRow("01", "FI")]
        [DataRow("02", "FI")]
        [DataRow("03", "FI")]
        [DataRow("04", "FI")]
        [DataRow("05", "FI")]
        [DataRow("06", "")]
        [DataRow("07", "")]
        [DataRow("08", "")]
        [DataRow("09", "")]
        [DataRow("10", "")]
        public void GetLatestAddress_ForDifferentAddressCodes_CountryFinnishOrEmptyString(string addressCode, string expectedCountry)
        {
            var xmlOnlyTemporaryAddress = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{addressCode}</code>
                            <text>Address</text>
                            <address>
                                <street>WeOnlyCareAboutTheStateName 5</street>
                                <zip>11340</zip>
                                <town>SomeTownWeDontCareAbout</town>
                            </address>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xmlOnlyTemporaryAddress, qType);
            var row = GetLatestAddressRow();
            var country = row.TwoLetterCountry;

            Assert.AreEqual(expectedCountry, country);
        }

        [TestMethod]
        public void GetAddress_RegardlessOfAddress_ShouldNotThrow()
        {
            var temporaryAddressInFinlandCode = "01";
            var permanentAddressAbroadCode = "06";
            var xmlWithoutAnyAddresses = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>00</count>
                    </addressData>
                </personData>
            </populationInformation>";

            var xmlWithTemporaryAddress = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{temporaryAddressInFinlandCode}</code>
                            <text>Temporary address</text>
                            <address>
                                <street>Santavuorentie 1 C 25</street>
                                <zip>00400</zip>
                                <town>Helsinki</town>
                            </address>
                            <stateCode></stateCode>
                            <stateName></stateName>
                            <startDate>2008-10-10</startDate>
                            <endDate>2012-10-10</endDate>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var xmlWithPermanentAbroad = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{permanentAddressAbroadCode}</code>
                            <text>Address</text>
                            <address>
                                <street>Santavuorentie 1 C 25</street>
                                <zip>00400</zip>
                                <town>Helsinki</town>
                            </address>
                            <stateCode></stateCode>
                            <stateName></stateName>
                            <startDate>2008-10-10</startDate>
                            <endDate>2012-10-10</endDate>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            // Try call to ensure no exception is called. Do nothing with the response, but test with different indata to Initiate. 
            try
            {
                Initiate(xmlWithoutAnyAddresses, qType);
                GetLatestAddressRow();

                Initiate(xmlWithTemporaryAddress, qType);
                GetLatestAddressRow();

                Initiate(xmlWithPermanentAbroad, qType);
                GetLatestAddressRow();

            }
            catch
            {
                Assert.Fail("GetAddress should never throw any exception. ");
            }

        }

        [TestMethod]
        public void Supervision_WhenIsLegallyIncompetent()
        {
            var xmlWithSupervision = $@"
            <populationInformation>
                <personData>
                    <supervisionDataRow>
                        <supervisionOfInterests>1</supervisionOfInterests>
                        <supervisionOfInterestsText>Interests' supervisor has been appointed to the person.</supervisionOfInterestsText>
                        <restrictionOnTheCompetenceToAct>3</restrictionOnTheCompetenceToAct>
                        <restrictionOnTheCompetenceToActText>The person has been declared legally incompetent.</restrictionOnTheCompetenceToActText>
                        <dutySeparationCode></dutySeparationCode>
                        <startDate>2000-09-09</startDate>
                        <supervisionRow>
                            <code>3</code>
                            <id>092</id>
                            <name>Vantaa</name>
                            <startDate>2000-09-09</startDate>
                        </supervisionRow>
                    </supervisionDataRow>
                </personData>
            </populationInformation>";

            var qType = "37";
            Initiate(xmlWithSupervision, qType);

            var restriction = RestrictionOfCompetenceToAct();
            var startDate = SupervisionStartDate();

            Assert.AreEqual("Declared legally incompetent", restriction);
            Assert.AreEqual("2000-09-09", startDate);
        }

        [TestMethod]
        public void Supervision_WithSUpervisionRowButNotRestricted()
        {
            var xmlWithSupervision = $@"
            <populationInformation>
                <personData>
                    <supervisionDataRow>
					<supervisionOfInterests>1</supervisionOfInterests>
					<supervisionOfInterestsText>Interests' supervisor has been appointed to the person.</supervisionOfInterestsText>
					<restrictionOnTheCompetenceToAct>3</restrictionOnTheCompetenceToAct>
					<restrictionOnTheCompetenceToActText>The person has been declared legally incompetent.</restrictionOnTheCompetenceToActText>
					<dutySeparationCode/>
					<supervisionRow>
						<code>3</code>
						<id>091</id>
						<name>Helsinki</name>
						<startDate>2000-01-01</startDate>
					</supervisionRow>
					<supervisionRow>
						<code>2</code>
						<id>091</id>
						<name>Helsinki</name>
						<startDate>2000-01-01</startDate>
					</supervisionRow>
				</supervisionDataRow>
                </personData>
            </populationInformation>";

            var qType = "37";
            Initiate(xmlWithSupervision, qType);

            var result = RestrictionOfCompetenceToAct();

            Assert.AreEqual("Declared legally incompetent", result);
        }

        [TestMethod]
        public void Addresses_CanSelectBothPermanentAndPostalAddressFromXml()
        {
            var xmlWithoutNationalityDataRows = $@"
            <populationInformation>
                <personData>
                <addressData>
                    <count>02</count>
                    <addressRow>
                        <code>{PermanentCode}</code>
                        <text>Address</text>
                        <address>
                            <street>Uusi osoite 190514</street>
                            <zip>00100</zip>
                            <town>Helsinki</town>
                        </address>
                        <stateCode></stateCode>
                        <stateName></stateName>
                        <startDate>2014-05-19</startDate>
                    </addressRow>
                    <addressRow>
                        <code>{PostalCode}</code>
                        <text>Address</text>
                        <address>
                            <street>PL 15</street>
                            <zip>00101</zip>
                            <town>Helsinki</town>
                        </address>
                        <stateCode>246</stateCode>
                        <stateName>Finland</stateName>
                        <startDate>2019-06-01</startDate>
                    </addressRow>
                </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xmlWithoutNationalityDataRows, qType);
            var postalAddress = GetLatestAddressRow(PostalCode);
            var permanentAddress = GetLatestAddressRow(PermanentCode);

            Assert.AreNotEqual("", postalAddress.ToString());
            Assert.AreNotEqual("", permanentAddress.ToString());
        }

        [TestMethod]
        public void CreditInfoSummary_CanReadMultipleValuesFromSummaryProperly()
        {
            var xmlWithSummary = $@"
            <consumerResponse>
                <creditInformationData>
                    <creditInformationEntryCount>2</creditInformationEntryCount>
                    <paymentDefaultCount>0</paymentDefaultCount>
                    <creditInformationEntrySummary>
                        <creditInformationEntries>true</creditInformationEntries>
                        <paymentDefaults>false</paymentDefaults>
                        <supervision>true</supervision>
                        <ownCreditStoppage>true</ownCreditStoppage>
                        <banOfBusiness>false</banOfBusiness>
                        <personInCharge>false</personInCharge>
                        <text></text>
                    </creditInformationEntrySummary>
                </creditInformationData>
            </consumerResponse>";

            var qType = "41";

            Initiate(xmlWithSummary, qType);
            var yes = "Yes";
            var no = "No";

            Assert.AreEqual(yes, HasCreditInformationEntries());
            Assert.AreEqual(no, HasPaymentDefaultEntries());
            Assert.AreEqual(yes, UnderSupervision());
            Assert.AreEqual(yes, OwnCreditStoppage());
            Assert.AreEqual(no, BanOfBusiness());
            Assert.AreEqual(no, ParticipatesInCompanies());
        }

        [TestMethod]
        public void NotInCreditRegister_ReturnNoWhenSATDefinesPersonDoesNotExistInTheirRegister()
        {
            var xmlWithSummary = $@"
            <response>
                <consumerResponse>
                    <noRegisteredMessage>
                        <EIR>EIR</EIR>
                        <EIRText>The person is not found in the register.</EIRText>
                    </noRegisteredMessage>
                </consumerResponse>
            </response>";

            var qType = "41";

            Initiate(xmlWithSummary, qType);
            var result = ExistsInRegister();

            Assert.AreEqual("No", result);
        }

        [TestMethod]
        public void AddressWhenDefaulted_ShouldNotThrowExceptionWhenAddressIsNonExisting()
        {
            var xmlWithSummary = $@"
            <response>
                <consumerResponse>
                    <personIdentification>
                        <name>Hammarberg Taneli </name>
                        <personId>020176-333E</personId>
                    </personIdentification>
                </consumerResponse>
            </response>";

            var qType = "41";

            Initiate(xmlWithSummary, qType);
            var result = GetRegisteredAddressForPaymentDefault();

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void AddressWhenDefaulted_FullAddressWhenExisting()
        {
            var xmlWithSummary = $@"
            <response>
                <consumerResponse>
                    <personIdentification>
                        <name>Hammarberg Taneli </name>
                        <personId>020176-333E</personId>
                        <addressWhenDefaulted>
                            <street>Poste Restante</street>
                            <zip>00100</zip>
                            <town>Helsinki</town>
                        </addressWhenDefaulted>
                    </personIdentification>
                </consumerResponse>
            </response>";

            var qType = "41";

            Initiate(xmlWithSummary, qType);
            var result = GetRegisteredAddressForPaymentDefault();

            Assert.AreEqual("Poste Restante, 00100, Helsinki", result);
        }

        [TestMethod]
        public void GetDomesticAddressSinceDate_WithStartDate_ShouldReturnStartDate()
        {
            var expectedDate = "2014-05-19";
            var xml = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{PermanentCode}</code>
                            <text>Address</text>
                            <address>
                                <street>Uusi osoite 190514</street>
                                <zip>00100</zip>
                                <town>Helsinki</town>
                            </address>
                            <startDate>{expectedDate}</startDate>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = GetDomesticAddressSinceDate();

            Assert.AreEqual(expectedDate, result);
        }

        [TestMethod]
        public void GetDomesticAddressSinceDate_WithoutStartDate_ShouldReturnNull()
        {
            var xml = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{PermanentCode}</code>
                            <text>Address</text>
                            <address>
                                <street>Uusi osoite 190514</street>
                                <zip>00100</zip>
                                <town>Helsinki</town>
                            </address>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = GetDomesticAddressSinceDate();

            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void GetDomesticAddressSinceDate_WithOtherThanPermanentAddress_ShouldReturnNull()
        {
            var xml = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{PostalCode}</code>
                            <text>Address</text>
                            <address>
                                <street>Poste Restante</street>
                                <zip>00100</zip>
                                <town>Helsinki</town>
                            </address>
                            <startDate>1998-01-01</startDate>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = GetDomesticAddressSinceDate();

            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void AddressRow_WhenStreetIsEmpty_ShouldNotThrowException()
        {
            var xml = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>01</count>
                        <addressRow>
                            <code>{PostalCode}</code>
                            <text>Address</text>
                            <address>
                                <zip>00100</zip>
                                <town>Helsinki</town>
                            </address>
                            <startDate>1998-01-01</startDate>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = ActivePosteRestanteAddress();

            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void GetDomesticAddressSinceDate_WithPermanentAndPosteRestante_ShouldReturPermanentStartDate()
        {
            var expectedDate = "2011-03-05";
            var xml = $@"
            <populationInformation>
                <personData>
                    <addressData>
                        <count>02</count>
                        <addressRow>
                            <code>{PermanentCode}</code>
                            <text>Address</text>
                            <address>
                                <street>Uusi osoite 190514</street>
                                <zip>00100</zip>
                                <town>Helsinki</town>
                            </address>
                            <startDate>{expectedDate}</startDate>
                        </addressRow>
                        <addressRow>
                            <code>{PostalCode}</code>
                            <text>Address</text>
                            <address>
                                <street>Poste Restante</street>
                                <zip>00100</zip>
                                <town>Helsinki</town>
                            </address>
                            <startDate>1998-01-01</startDate>
                        </addressRow>
                    </addressData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = GetDomesticAddressSinceDate();

            Assert.AreEqual(expectedDate, result);
        }

        [TestMethod]
        public void GetDomesticAddressSinceDate_WhenNoAddressAtAll_ShouldReturnNull()
        {
            var xml = $@"
            <populationInformation>
                <personData>
                </personData>
            </populationInformation>";

            var qType = "37";

            Initiate(xml, qType);
            var result = GetDomesticAddressSinceDate();

            Assert.AreEqual(null, result);
        }

    }
}

