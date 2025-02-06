using nCreditReport.Models;
using nCreditReport.RandomDataSource;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nCreditReport.Code
{
    public class TestOnlyCreditReportService : PersonBaseCreditReportService
    {
        private readonly string forCountry;
        private readonly IDocumentClient documentClient;

        public TestOnlyCreditReportService(string providerName, string forCountry, IDocumentClient documentClient) : base(providerName)
        {
            this.forCountry = forCountry;
            this.documentClient = documentClient;
        }

        public override string ForCountry
        {
            get
            {
                return this.forCountry;
            }
        }

        private Dictionary<string, IDictionary<string, string>> testPersonCache = new Dictionary<string, IDictionary<string, string>>();

        public IDictionary<string, string> GetTestPerson(ICivicRegNumber civicRegNr)
        {
            if (!testPersonCache.ContainsKey(civicRegNr.NormalizedValue))
            {
                var c = new nTestClient();
                var r = c.GetOrGenerateTestPersons(new List<nTestClient.GetOrGenerateTestPersonModel>
                {
                    new nTestClient.GetOrGenerateTestPersonModel
                    {
                        CivicRegNr = civicRegNr.NormalizedValue,
                        IsAccepted = true,
                        Customizations = new Dictionary<string, string>()
                    }
                }, true);

                var result = r[civicRegNr.NormalizedValue];

                if (result == null)
                    throw new Exception("This should be impossible. Bug in nTest");
                testPersonCache[civicRegNr.NormalizedValue] = result.Properties;
            }
            return testPersonCache[civicRegNr.NormalizedValue];
        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            var result = GetTestPerson(civicRegNr);

            if (result != null)
            {
                var r = CreateResult(civicRegNr, result.Select(x => new SaveCreditReportRequest.Item
                {
                    Name = x.Key.StartsWith("creditreport_") ? x.Key.Substring("creditreport_".Length) : x.Key,
                    Value = x.Value
                }), requestData);

                if (forCountry == "SE")
                {
                    var htmlReportArchiveKey = r?.Items?.Where(x => x.Name == "htmlReportArchiveKey")?.FirstOrDefault()?.Value;
                    var templateName = r?.Items?.Where(x => x.Name == "templateName")?.FirstOrDefault()?.Value;
                    if (htmlReportArchiveKey == null)
                    {
                        var archiveKey = this.documentClient.ArchiveStore(Encoding.UTF8.GetBytes(EmbeddedResources.LoadFileAsString("UcMicroExampleReport.html")), "text/html", $"uc3_{civicRegNr.NormalizedValue}.html");
                        r.Items.Add(new SaveCreditReportRequest.Item
                        {
                            Name = "htmlReportArchiveKey",
                            Value = archiveKey
                        });
                    }
                }

                return new Result
                {
                    IsError = false,
                    IsInvalidCredentialsError = false,
                    ErrorMessage = null,
                    CreditReport = r
                };
            }
            else
            {
                return new Result
                {
                    IsError = false,
                    IsInvalidCredentialsError = false,
                    ErrorMessage = null,
                    CreditReport = CreateResult(civicRegNr, new List<SaveCreditReportRequest.Item>
                        {
                            new SaveCreditReportRequest.Item
                            {
                                Name = "personStatus",
                                Value = "nodata"
                            }
                        }, requestData)
                };
            }
        }

        public override List<DictionaryEntry> FetchTabledValues(CreditReportRepository.FetchResult creditReport)
        {
            var results = new List<DictionaryEntry>();
            var creditReportFields = NEnv.CreditReportFields(ProviderName);

            void AddValue(string name, string value) => results.Add(new DictionaryEntry(name, value));

            AddValue("Credit report provider", ProviderName);

            // For every field that is saved in the database and exists in the CreditReport-Fields.json, add them to the resultlist and return. 
            foreach (var availableField in creditReport.Items)
            {
                var setting = creditReportFields.SingleOrDefault(f => f.Field == availableField.Name);
                if (setting != null)
                {
                    AddValue(setting.Title, availableField.Value);
                }
            }

            return results;
        }

        public override bool CanFetchTabledValues() => true;
    }
}