using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code
{
    public class CivicRegNrSwapTestCreditReportService : PersonBaseCreditReportService
    {
        public override string ForCountry => testProvider.ForCountry;

        private readonly PersonBaseCreditReportService realProvider;
        private readonly TestOnlyCreditReportService testProvider;

        public CivicRegNrSwapTestCreditReportService(PersonBaseCreditReportService realProvider, TestOnlyCreditReportService testProvider) : base(testProvider.ProviderName)
        {
            this.realProvider = realProvider;
            this.testProvider = testProvider;
        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            if (NEnv.IsProduction)
                throw new Exception("Not allowed in production");

            var testPersonData = testProvider.GetTestPerson(civicRegNr);
            if (!testPersonData.ContainsKey("creditReportCivicRegNr"))
                return testProvider.TryBuyCreditReport(civicRegNr, requestData);

            var subCivicRegNr = new CivicRegNumberParser(ForCountry).Parse(testPersonData["creditReportCivicRegNr"]);

            var replacementItems = new Dictionary<string, string>();
            var replaceMentRegisteredMunicipality = testPersonData.Opt("creditReportRegisteredMunicipality");
            if (replaceMentRegisteredMunicipality != null)
                replacementItems["registeredMunicipality"] = replaceMentRegisteredMunicipality;

            var result = realProvider.TryBuyCreditReport(subCivicRegNr, requestData);

            var r = new Result
            {
                ErrorMessage = result.ErrorMessage,
                IsError = result.IsError,
                IsInvalidCredentialsError = result.IsInvalidCredentialsError,
                IsTimeoutError = result.IsTimeoutError
            };
            var c = result.CreditReport;
            if (c != null)
            {
                r.CreditReport = new SaveCreditReportRequest
                {
                    ChangedById = c.ChangedById,
                    CreationDate = c.CreationDate,
                    CreditReportProviderName = testProvider.ProviderName,
                    InformationMetaData = c.InformationMetaData,
                    RequestDate = c.RequestDate,
                    SearchTerms = HandleSearchTerms(civicRegNr, testProvider.ProviderName, c.SearchTerms),
                    Items = HandleItems(civicRegNr, c.Items, replacementItems)
                };
            }

            return r;
        }

        private List<SaveCreditReportRequest.Item> HandleSearchTerms(ICivicRegNumber originalCivicRegNr, string originalProviderName, List<SaveCreditReportRequest.Item> items)
        {
            return items.Select(x =>
            {
                if (x.Name == "birthDate")
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = originalCivicRegNr.BirthDate.Value.ToString("yyyy-MM-dd") };
                else if (x.Name == "providerName")
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = originalProviderName };
                else
                    return x;
            }).ToList();
        }

        private List<SaveCreditReportRequest.Item> HandleItems(ICivicRegNumber originalCivicRegNr, List<SaveCreditReportRequest.Item> items, Dictionary<string, string> replacementItems)
        {
            return items.Select(x =>
            {
                if (x.Name == "civicRegNr")
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = originalCivicRegNr.NormalizedValue };
                else if (replacementItems.ContainsKey(x.Name))
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = replacementItems[x.Name] };
                else
                    return x;
            }).ToList();
        }
    }
}