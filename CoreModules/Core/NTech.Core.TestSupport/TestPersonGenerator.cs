using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class TestPersonGenerator
    {
        private Dictionary<SampleDataFieldCode, List<string>> sampleData = null;
        private bool useFiReformedCenturyMarker;

        private TestPersonGenerator(string baseCountry, bool useFiReformedCenturyMarker)
        {
            BaseCountry = baseCountry;
            this.useFiReformedCenturyMarker = useFiReformedCenturyMarker;
        }

        public string BaseCountry { get; private set; }

        private static Dictionary<string, TestPersonGenerator> sharedInstanceByCountry = new Dictionary<string, TestPersonGenerator>();

        public static TestPersonGenerator GetSharedInstance(string baseCountry, bool useFiReformedCenturyMarker = false)
        {
            var cacheKey = useFiReformedCenturyMarker ? baseCountry + "_R" : baseCountry;

            if (!sharedInstanceByCountry.ContainsKey(cacheKey))
            {
                var g = new TestPersonGenerator(baseCountry, useFiReformedCenturyMarker);
                g.LoadSampleData();
                sharedInstanceByCountry[cacheKey] = g;
            }

            return sharedInstanceByCountry[cacheKey];
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

        public ICivicRegNumber GenerateCivicRegNumber(IRandomnessSource random, DateTime? customBirthDate = null)
        {
            var g = new CivicRegNumberGenerator(BaseCountry);
            g.UseReformedFinnishCenturyMarkers = useFiReformedCenturyMarker;
            var p = new CivicRegNumberParser(BaseCountry);
            DateTime birthDate;

            if (customBirthDate.HasValue)
                birthDate = customBirthDate.Value;
            else
            {
                var birthYear = random.NextIntBetween(1970, 1990);
                var birthMonth = random.NextIntBetween(1, 12);
                birthDate = new DateTime(birthYear, birthMonth, 1).AddDays(random.NextIntBetween(0, 30)); //Might rollover the month but it doesnt matter
            }

            return g.Generate(new Random(random.NextIntBetween(0, 10000000)), birthDate);
        }

        public Dictionary<string, string> GenerateTestPerson(IRandomnessSource random, ICivicRegNumber newCivicRegNr, bool isAccepted, DateTime now)
        {
            var p = new Dictionary<string, string>();

            //Civic nr
            p["civicRegNr"] = newCivicRegNr.NormalizedValue;
            p["civicRegNrCountry"] = newCivicRegNr.Country;
            p["birthDate"] = newCivicRegNr.BirthDate.Value.ToString("yyyy-MM-dd");

            //Seed data
            foreach (var c in Enum.GetValues(typeof(SampleDataFieldCode)).Cast<SampleDataFieldCode>())
            {
                var randomSample = sampleData[c][random.NextIntBetween(0, sampleData[c].Count - 1)];

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
            }

            //Scoring
            InjectStandardCreditReport(isAccepted, newCivicRegNr, random, p);
            if (newCivicRegNr.Country == "FI")
            {
                InjectSatFiCreditReport(isAccepted, p, now, random);
            }

            //Bank account
            var g = new BankAccountGenerator();
            if (newCivicRegNr.Country == "FI")
            {
                p["iban"] = g.GenerateIbanFi(random).NormalizedValue;
            }
            else if (newCivicRegNr.Country == "SE")
            {
                p["bankAccountNr"] = g.GenerateSwedishBankAccountNr(newCivicRegNr).FormatFor(null);
            }

            return p;
        }

        private static void InjectStandardCreditReport(bool isAccepted, ICivicRegNumber newCivicRegNr, IRandomnessSource random, IDictionary<string, string> p)
        {
            if (isAccepted)
            {
                p["creditreport_nrOfPaymentRemarks"] = "0";
                p["creditreport_hasPaymentRemark"] = "false";
                p["creditreport_hasDomesticAddress"] = "true";
                p["creditreport_domesticAddressSinceDate"] = newCivicRegNr.BirthDate.Value.AddYears(-30).ToString("yyyy-MM-dd");
                p["creditreport_personStatus"] = "normal";
                p["creditreport_hasPostBoxAddress"] = "false";
                p["creditreport_hasPosteRestanteAddress"] = "false";
                p["creditreport_hasGuardian"] = "false";
            }
            else
            {
                p["creditreport_nrOfPaymentRemarks"] = random.NextIntBetween(1, 9).ToString();
                p["creditreport_hasPaymentRemark"] = "true";
                p["creditreport_hasDomesticAddress"] = "true";
                p["creditreport_hasBusinessConnection"] = "true";
                p["creditreport_personStatus"] = "normal";
                p["creditreport_hasPostBoxAddress"] = "false";
                p["creditreport_hasPosteRestanteAddress"] = "false";
                p["creditreport_hasGuardian"] = "false";
            }

            if (newCivicRegNr.Country == "FI")
            {
                if (isAccepted)
                {
                    p["creditreport_bricRiskOfPaymentRemark"] = "Small";
                    p["creditreport_hasBusinessConnection"] = "true";

                }
                else
                {
                    p["creditreport_bricRiskOfPaymentRemark"] = "High";
                    p["creditreport_hasBusinessConnection"] = "true";
                }
            }
            else if (newCivicRegNr.Country == "SE")
            {
                p["creditreport_templateAccepted"] = isAccepted ? "true" : "false";
                p["creditreport_templateName"] = isAccepted ? "3" : "90";
                p["creditreport_templateManualAttention"] = "false";
                if (isAccepted)
                {
                    p["creditreport_riskPercent"] = "1.3";
                    p["creditreport_riskValue"] = "1.3";
                    p["creditreport_latestIncomePerYear"] = "1200000";
                }
                else
                {
                    p["creditreport_templateReasonCode"] = "FRVSKSASKAN2";
                    p["creditreport_latestIncomePerYear"] = "60000";
                }
                p["creditreport_isRecentCitizen"] = "false";
                p["creditreport_hasRegisteredMunicipality"] = "true";
                p["creditreport_registeredMunicipality"] = "Stockholm";
                p["creditreport_hasAddressChange"] = "false";
            }
        }

        private static void InjectSatFiCreditReport(bool isAccepted, IDictionary<string, string> p, DateTime now, IRandomnessSource random)
        {
            Action<string, string> add = (name, value) => p[$"satfi_{name}"] = value;

            if (isAccepted)
            {
                //Based on the SAT testperson 191148-999R
                add("count", "1");
                add("c15", "1");
                add("h14", "0");
                add("d11", "0");
                add("d12", "0");
                add("e11", "0");
                add("e12", "0");
                add("f11", "0");
                add("f12", "0");
                add("f13", "0");
                add("h15", "0");
                add("h16", "0");
                add("k11", now.AddMonths(-random.NextIntBetween(20, 60)).ToString("yyyy-MM-dd")); //Most recent
                add("k12", ""); //They actually reply with empty string rather than not sending the row ... very strange ws
                add("c01", "30000");
                add("c02", "0");
                add("c03", "0");
                add("c04", "1200");
                add("test_istimeout", "false");
            }
            else
            {
                //Based on the SAT testperson 211060-9591
                add("count", "4");
                add("c15", "4");
                add("h14", "179");
                add("d11", "2");
                add("d12", "820");
                add("e11", "0");
                add("e12", "0");
                add("f11", "1");
                add("f12", "100");
                add("f13", "100");
                add("h15", "3");
                add("h16", "0");
                add("k11", now.AddDays(-random.NextIntBetween(5, 25)).ToString("yyyy-MM-dd"));
                add("k12", "2015-05-03");
                add("c01", "2150");
                add("c02", "0");
                add("c03", "10");
                add("c04", "400");
                add("test_istimeout", "false");
            }
        }
    }
}