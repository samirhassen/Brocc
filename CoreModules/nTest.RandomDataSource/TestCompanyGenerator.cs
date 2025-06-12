using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Ionic.Zip;
using NTech.Banking.BankAccounts;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;

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

        private static TestCompanyGenerator _sharedInstance;
        private static readonly object SharedInstanceLock = new object();

        public static TestCompanyGenerator GetSharedInstance(string baseCountry)
        {
            if (_sharedInstance == null)
            {
                lock (SharedInstanceLock)
                {
                    if (_sharedInstance != null) return _sharedInstance;
                    _sharedInstance = new TestCompanyGenerator(baseCountry);
                    _sharedInstance.LoadSampleData();
                }
            }
            else if (_sharedInstance.BaseCountry != baseCountry)
                throw new NotImplementedException();

            return _sharedInstance;
        }

        private enum SampleDataFieldCode
        {
            firstName,
            lastName,
            addressStreet,
            addressZipcode,
            addressCity,
            phone,
            email
        }

        private void LoadSampleData()
        {
            sampleData = new Dictionary<SampleDataFieldCode, List<string>>();
            foreach (var c in Enum.GetValues(typeof(SampleDataFieldCode)).Cast<SampleDataFieldCode>())
            {
                var lines = EmbeddedResources.LoadAsLines(
                    $"personsampledata-{BaseCountry.ToLowerInvariant()}-{c.ToString()}.txt");
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

                switch (c)
                {
                    case SampleDataFieldCode.addressStreet:
                        randomSample += $"{random.NextIntBetween(0, 999)}"; //Randomize the streetnumbers a bit
                        a.AddressStreet = randomSample;
                        break;
                    case SampleDataFieldCode.addressCity:
                        a.AddressCity = randomSample;
                        break;
                    case SampleDataFieldCode.addressZipcode:
                        a.AddressZipcode = randomSample;
                        break;
                }
            }

            return a;
        }

        public Dictionary<string, string> GenerateTestCompany(IRandomnessSource random, IOrganisationNumber newOrgnr,
            bool isAccepted, DateTime now, Random r, BankAccountNumberTypeCode bankAccountType,
            Func<Stream, string> storeHtmlDocumentInArchive)
        {
            var p = new Dictionary<string, string>
            {
                //Civic nr
                ["orgnr"] = newOrgnr.NormalizedValue,
                ["orgnrCountry"] = newOrgnr.Country
            };

            //Seed data
            foreach (var c in Enum.GetValues(typeof(SampleDataFieldCode)).Cast<SampleDataFieldCode>()
                         .Where(x => x != SampleDataFieldCode.firstName && x != SampleDataFieldCode.lastName))
            {
                var randomSample = GetRandomData(c);

                switch (c)
                {
                    case SampleDataFieldCode.addressStreet:
                        randomSample += $"{random.NextIntBetween(0, 999)}"; //Randomize the streetnumbers a bit
                        break;
                    case SampleDataFieldCode.email:
                    {
                        var parts = randomSample.Split('@');
                        randomSample = parts[0] + random.NextIntBetween(1, 99999) + "@" +
                                       string.Join("@", parts.Skip(1));
                        break;
                    }
                    case SampleDataFieldCode.phone:
                        randomSample =
                            $"{random.NextIntBetween(0, 99).ToString().PadLeft(3, '0')} {random.NextIntBetween(0, 999).ToString().PadLeft(3, '0')} {random.NextIntBetween(0, 9999).ToString().PadLeft(4, '0')}";
                        break;
                }

                p[c.ToString()] = randomSample;

                if (c == SampleDataFieldCode.addressCity || c == SampleDataFieldCode.addressStreet ||
                    c == SampleDataFieldCode.addressZipcode)
                    p[$"creditreport_{c.ToString()}"] = randomSample;
            }

            var name =
                $"{GetRandomData(SampleDataFieldCode.firstName)} {GetRandomData(SampleDataFieldCode.lastName)} {(newOrgnr.Country == "FI" ? "Oy" : "AB")}";

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
            var accountNr = g.Generate(bankAccountType, random,
                () => new CivicRegNumberGenerator(newOrgnr.Country).Generate(r));
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

            string GetRandomData(SampleDataFieldCode x) =>
                sampleData[x][random.NextIntBetween(0, sampleData[x].Count - 1)];
        }

        private Dictionary<string, string> CreateUcSeTestCreditReport(IRandomnessSource random, bool isAccepted,
            Func<Stream, string> storeHtmlDocumentInArchive)
        {
            var companyCreditReportHtmlArchiveKey = EmbeddedResources.UsingStream("exempel_foretagsuc.zip", s =>
            {
                using (var zip = ZipFile.Read(s))
                {
                    var report = zip.Entries.First(x => x.FileName.EndsWith(".html"));
                    using (var r = report.OpenReader())
                    {
                        return storeHtmlDocumentInArchive(r);
                    }
                }
            });

            var styrelseMaxManader = (isAccepted ? random.NextIntBetween(20, 100) : random.NextIntBetween(1, 5));
            var result = new Dictionary<string, string>
            {
                ["htmlReportArchiveKey"] = companyCreditReportHtmlArchiveKey,
                ["foretagAlderIManader"] =
                    (isAccepted ? random.NextIntBetween(10, 150) : random.NextIntBetween(1, 5)).ToString(),
                ["riskklassForetag"] =
                    isAccepted ? random.OneOf("2", "3", "4", "5") : random.OneOf("1", "1", "1", "K", "O", "U"),
                ["bolagsform"] = "aktiebolag",
                ["bolagsstatus"] = isAccepted ? "Ok" : random.OneOf("A", "K", "L", "R"),
                ["summaEgetKapital"] = isAccepted ? "100000000" : "0",
                ["soliditetProcent"] = Math
                    .Round(isAccepted ? random.NextDecimal(88m, 99.99m) : random.NextDecimal(0.1m, 1.9m), 2)
                    .ToString(CultureInfo.InvariantCulture),
                ["styrelseLedamotMaxMander"] = styrelseMaxManader.ToString(),
                ["antalStyrelseLedamotsManader"] =
                    (isAccepted ? styrelseMaxManader * random.NextIntBetween(1, 3) : styrelseMaxManader).ToString(),
                ["antalAnmarkningar"] = (isAccepted ? 0 : random.NextIntBetween(1, 10)).ToString(),
                ["antalModerbolag"] = "0",
                ["moderbolagRiskklassForetag"] = "missing",
                ["nettoOmsattning"] = isAccepted ? "100000000" : "5000",
                ["nettoOmsattningFg"] = isAccepted ? "90000000" : "4000",
                ["bokslutDatum"] = $"{DateTime.Now.AddYears(-1).ToString("yyyy")}-12-31",
                ["summaObeskattadeReserver"] = "missing",
                ["summaImmateriellaTillgangar"] = "0",
                ["avkastningTotKapProcent"] = "2.43",
                ["kassalikviditetProcent"] = "101.32",
                ["riskprognosForetagProcent"] = "0.25",
                ["snikod"] = "68203",
                ["styrelseRevisorKod"] = isAccepted ? random.OneOf("Auktoriserad revisor", "Godkänd revisor") : "Ingen"
            };

            var ns = new List<string>
            {
                "finnsStyrelseKonkursengagemang",
                "finnsStyrelseBetAnmarkningar",
                "finnsStyrelseKonkursansokningar"
            };
            foreach (var n in ns)
                result[n] = isAccepted ? "false" : "true";

            return result;
        }
    }
}