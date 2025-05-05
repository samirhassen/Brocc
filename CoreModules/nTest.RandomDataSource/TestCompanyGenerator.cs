
using NTech.Banking.BankAccounts;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class TestCompanyGenerator
    {
        private Dictionary<SampleDataFieldCode, List<string>> sampleData = null;

        private TestCompanyGenerator(string baseCountry)
        {
            BaseCountry = baseCountry;
        }

        public string BaseCountry { get; private set; }

        private static TestCompanyGenerator sharedInstance;
        private static object sharedInstanceLock = new object();
        public static TestCompanyGenerator GetSharedInstance(string baseCountry)
        {
            if (sharedInstance == null)
            {
                lock (sharedInstanceLock)
                {
                    if (sharedInstance == null)
                    {
                        sharedInstance = new TestCompanyGenerator(baseCountry);
                        sharedInstance.LoadSampleData();
                    }
                }
            }
            else if (sharedInstance.BaseCountry != baseCountry)
                throw new NotImplementedException();

            return sharedInstance;
        }

        private enum SampleDataFieldCode
        {
            firstName, lastName, addressStreet, addressZipcode, addressCity, phone, email
        }

        private void LoadSampleData()
        {
            sampleData = new Dictionary<SampleDataFieldCode, List<string>>();
            foreach (var c in Enum.GetValues(typeof(SampleDataFieldCode)).Cast<SampleDataFieldCode>())
            {
                var lines = EmbeddedResources.LoadAsLines($"personsampledata-{BaseCountry.ToLowerInvariant()}-{c.ToString()}.txt");
                sampleData[c] = lines;
            }
        }

        public class Address
        {
            public string AddressStreet { get; set; }
            public string AddressZipcode { get; set; }
            public string AddressCity { get; set; }
        }

        public Address GenerateAddress(IRandomnessSource random)
        {
            var a = new Address();
            foreach (var c in Enum.GetValues(typeof(SampleDataFieldCode)).Cast<SampleDataFieldCode>())
            {
                var randomSample = sampleData[c][random.NextIntBetween(0, sampleData[c].Count - 1)];

                if (c == SampleDataFieldCode.addressStreet)
                {
                    randomSample += $"{random.NextIntBetween(0, 999)}"; //Randomize the streetnumbers a bit
                    a.AddressStreet = randomSample;
                }
                else if (c == SampleDataFieldCode.addressCity)
                {
                    a.AddressCity = randomSample;
                }
                else if (c == SampleDataFieldCode.addressZipcode)
                {
                    a.AddressZipcode = randomSample;
                }
            }
            return a;
        }

        public Dictionary<string, string> GenerateTestCompany(IRandomnessSource random, IOrganisationNumber newOrgnr, bool isAccepted, DateTime now, Random r, BankAccountNumberTypeCode bankAccountType, Func<Stream, string> storeHtmlDocumentInArchive)
        {
            var p = new Dictionary<string, string>();

            //Civic nr
            p["orgnr"] = newOrgnr.NormalizedValue;
            p["orgnrCountry"] = newOrgnr.Country;

            Func<SampleDataFieldCode, string> getRandomData = x => sampleData[x][random.NextIntBetween(0, sampleData[x].Count - 1)];

            //Seed data
            foreach (var c in Enum.GetValues(typeof(SampleDataFieldCode)).Cast<SampleDataFieldCode>().Where(x => x != SampleDataFieldCode.firstName && x != SampleDataFieldCode.lastName))
            {
                var randomSample = getRandomData(c);

                if (c == SampleDataFieldCode.addressStreet)
                {
                    randomSample += $"{random.NextIntBetween(0, 999)}"; //Randomize the streetnumbers a bit
                }
                else if (c == SampleDataFieldCode.email)
                {
                    var parts = randomSample.Split('@');
                    randomSample = parts[0] + random.NextIntBetween(1, 99999) + "@" + string.Join("@", parts.Skip(1));
                }
                else if (c == SampleDataFieldCode.phone)
                {
                    randomSample = $"{random.NextIntBetween(0, 99).ToString().PadLeft(3, '0')} {random.NextIntBetween(0, 999).ToString().PadLeft(3, '0')} {random.NextIntBetween(0, 9999).ToString().PadLeft(4, '0')}";
                }

                p[c.ToString()] = randomSample;

                if (c == SampleDataFieldCode.addressCity || c == SampleDataFieldCode.addressStreet || c == SampleDataFieldCode.addressZipcode)
                    p[$"creditreport_{c.ToString()}"] = randomSample;
            }

            var name = $"{getRandomData(SampleDataFieldCode.firstName)} {getRandomData(SampleDataFieldCode.lastName)} {(newOrgnr.Country == "FI" ? "Oy" : "AB")}";

            p["companyName"] = name;
            p["creditreport_companyName"] = name;

            //Credit report            

            if (newOrgnr.Country == "SE")
            {
                var report = CreateUcSeTestCreditReport(random, isAccepted, storeHtmlDocumentInArchive);
                foreach (var f in report)
                {
                    p[$"creditreport_{f.Key}"] = f.Value;
                }
            }

            //Bank account
            var g = new BankAccountGenerator();
            var accountNr = g.Generate(bankAccountType, random, () => new CivicRegNumberGenerator(newOrgnr.Country).Generate(r));
            if (bankAccountType == BankAccountNumberTypeCode.IBANFi)
            {
                p["iban"] = accountNr.FormatFor(null);
            }
            else
            {
                p["bankAccountNr"] = accountNr.FormatFor(null);
            }
            p["bankAccountNrType"] = bankAccountType.ToString();

            return p;
        }
        public Stream ConvertStreamReaderToStream(StreamReader reader)
        {
            // Access the underlying stream from the StreamReader
            return reader.BaseStream;
        }
        private Dictionary<string, string> CreateUcSeTestCreditReport(IRandomnessSource random, bool isAccepted, Func<Stream, string> storeHtmlDocumentInArchive)
        {
            var result = new Dictionary<string, string>();

            var companyCreditReportHtmlArchiveKey = EmbeddedResources.UsingStream("exempel_foretagsuc.zip", s =>
            {
                

                using (var archive = new ZipArchive(s, ZipArchiveMode.Read))
                {
                    var reportEntry = archive.Entries.FirstOrDefault(x => x.FullName.EndsWith(".html"));
                    using (var reader = new StreamReader(reportEntry.Open()))
                    {
                        return storeHtmlDocumentInArchive(ConvertStreamReaderToStream(reader));
                    }
                }


            });

            result["htmlReportArchiveKey"] = companyCreditReportHtmlArchiveKey;
            result["foretagAlderIManader"] = (isAccepted ? random.NextIntBetween(10, 150) : random.NextIntBetween(1, 5)).ToString();
            result["riskklassForetag"] = isAccepted ? random.OneOf("2", "3", "4", "5") : random.OneOf("1", "1", "1", "K", "O", "U");
            result["bolagsform"] = "aktiebolag";
            result["bolagsstatus"] = isAccepted ? "Ok" : random.OneOf("A", "K", "L", "R");
            result["summaEgetKapital"] = isAccepted ? "100000000" : "0";
            result["soliditetProcent"] = Math.Round(isAccepted ? random.NextDecimal(88m, 99.99m) : random.NextDecimal(0.1m, 1.9m), 2).ToString(CultureInfo.InvariantCulture);
            var styrlseMaxMander = (isAccepted ? random.NextIntBetween(20, 100) : random.NextIntBetween(1, 5));
            result["styrelseLedamotMaxMander"] = styrlseMaxMander.ToString();
            result["antalStyrelseLedamotsManader"] = (isAccepted ? styrlseMaxMander * random.NextIntBetween(1, 3) : styrlseMaxMander).ToString();
            result["antalAnmarkningar"] = (isAccepted ? 0 : random.NextIntBetween(1, 10)).ToString();
            result["antalModerbolag"] = "0";
            result["moderbolagRiskklassForetag"] = "missing";
            result["nettoOmsattning"] = isAccepted ? "100000000" : "5000";
            result["nettoOmsattningFg"] = isAccepted ? "90000000" : "4000";
            result["bokslutDatum"] = $"{DateTime.Now.AddYears(-1).ToString("yyyy")}-12-31";
            result["summaObeskattadeReserver"] = "missing";
            result["summaImmateriellaTillgangar"] = "0";
            result["avkastningTotKapProcent"] = "2.43";
            result["kassalikviditetProcent"] = "101.32";
            result["riskprognosForetagProcent"] = "0.25";
            result["snikod"] = "68203";
            result["styrelseRevisorKod"] = isAccepted ? random.OneOf("Auktoriserad revisor", "Godkänd revisor") : "Ingen";
            foreach (var n in new List<string> { "finnsStyrelseKonkursengagemang", "finnsStyrelseBetAnmarkningar", "finnsStyrelseKonkursansokningar" })
                result[n] = isAccepted ? "false" : "true";

            return result;
        }
    }
}