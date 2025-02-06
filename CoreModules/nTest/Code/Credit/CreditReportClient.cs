using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nTest.Code.Credit
{
    public class CreditReportClient
    {
        private NHttp.NHttpCall Begin(TimeSpan? timeout = null)
        {
            return NHttp.Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nCreditReport"), NEnv.AutomationBearerToken(), timeout: timeout);
        }

        public ReportResponse BuyCreditReport(ICivicRegNumber civicRegNr, int customerId, IList<string> requestedCreditReportFields, string providerName, bool forceBuyNew, bool returnFieldsOnNew, Dictionary<string, string> additionalParameters)
        {
            var reports = Begin()
                .PostJson("CreditReport/Find", new { providerName = providerName, customerId = customerId })
                .ParseJsonAs<List<CreditReportFindHit>>();

            var latestReport = reports.OrderByDescending(x => x.RequestDate).FirstOrDefault();
            int? creditReportId = null;
            List<CreditReportItem> creditReportItems = null;
            bool isNew = false;
            if (latestReport == null || forceBuyNew)
            {
                //Buy a new one if none exist for from today
                var result = Begin(timeout: TimeSpan.FromSeconds(60))
                .PostJson("CreditReport/BuyNew",
                    new { providerName = providerName, civicRegNr = civicRegNr.NormalizedValue, customerId = customerId, returningItemNames = returnFieldsOnNew ? requestedCreditReportFields?.ToArray() : null, additionalParameters })
                    .ParseJsonAs<CreditReportBuyNewResult>();
                isNew = true;

                if (result == null)
                    return new ReportResponse
                    {
                        ProviderIsDown = true
                    };
                else if (result.IsInvalidCredentialsError || result.IsTimeoutError)
                    return new ReportResponse
                    {
                        IsInvalidCredentialsError = result.IsInvalidCredentialsError,
                        ProviderIsDown = result.IsTimeoutError
                    };
                else
                {
                    creditReportId = result.CreditReportId;
                    creditReportItems = result.Items;
                }
            }
            else
            {
                creditReportId = latestReport.CreditReportId;
                creditReportItems = Begin().PostJson("CreditReport/GetById", new
                {
                    creditReportId = creditReportId.Value,
                    itemNames = requestedCreditReportFields.ToArray()
                }).ParseJsonAs<CreditReportResult>().Items;
            }

            return new ReportResponse
            {
                CreditReportId = creditReportId,
                Items = creditReportItems,
                IsNewReport = isNew,
                ProviderIsDown = false,
                IsInvalidCredentialsError = false
            };
        }

        public ReportResponse BuyCompanyCreditReport(IOrganisationNumber orgnr, int customerId, IList<string> requestedCreditReportFields, string providerName, bool forceBuyNew, bool returnFieldsOnNew, Dictionary<string, string> additionalParameters)
        {
            var reports = Begin()
                .PostJson("CompanyCreditReport/Find", new { providerName = providerName, customerId = customerId })
                .ParseJsonAs<List<CreditReportFindHit>>();

            var latestReport = reports.OrderByDescending(x => x.RequestDate).FirstOrDefault();
            int? creditReportId = null;
            List<CreditReportItem> creditReportItems = null;
            bool isNew = false;
            if (latestReport == null || forceBuyNew)
            {
                //Buy a new one if none exist for from today
                var result = Begin(timeout: TimeSpan.FromSeconds(60))
                .PostJson("CompanyCreditReport/BuyNew",
                    new { providerName = providerName, orgnr = orgnr.NormalizedValue, customerId = customerId, returningItemNames = returnFieldsOnNew ? requestedCreditReportFields?.ToArray() : null, additionalParameters })
                    .ParseJsonAs<CreditReportBuyNewResult>();
                isNew = true;

                if (result == null)
                    return new ReportResponse
                    {
                        ProviderIsDown = true
                    };
                else if (result.IsInvalidCredentialsError || result.IsTimeoutError)
                    return new ReportResponse
                    {
                        IsInvalidCredentialsError = result.IsInvalidCredentialsError,
                        ProviderIsDown = result.IsTimeoutError
                    };
                else
                {
                    creditReportId = result.CreditReportId;
                    creditReportItems = result.Items;
                }
            }
            else
            {
                creditReportId = latestReport.CreditReportId;
                creditReportItems = Begin().PostJson("CompanyCreditReport/GetById", new
                {
                    creditReportId = creditReportId.Value,
                    itemNames = requestedCreditReportFields.ToArray()
                }).ParseJsonAs<CreditReportResult>().Items;
            }

            return new ReportResponse
            {
                CreditReportId = creditReportId,
                Items = creditReportItems,
                IsNewReport = isNew,
                ProviderIsDown = false,
                IsInvalidCredentialsError = false
            };
        }

        public class TabledValue
        {
            public string Title { get; set; }
            public string Value { get; set; }
        }

        public List<TabledValue> FetchTabledValues(int creditReportId)
        {
            var result = Begin(timeout: TimeSpan.FromSeconds(60))
                .PostJson("CreditReport/FetchTabledValues",
                    new { creditReportId = creditReportId })
                .ParseJsonAs<List<TabledValue>>();

            return result;
        }

        public class ReportResponse
        {
            public int? CreditReportId { get; set; }
            public List<CreditReportItem> Items { get; set; }
            public bool ProviderIsDown { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public bool IsNewReport { get; set; }
        }

        public class CreditReportItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private class CreditReportResult
        {
            public DateTimeOffset RequestDate { get; set; }
            public int CreditReportId { get; set; }
            public List<CreditReportItem> Items { get; set; }
        }

        private class CreditReportBuyNewResult
        {
            public bool Success { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public int CreditReportId { get; set; }
            public List<CreditReportItem> Items { get; set; }
            public bool IsTimeoutError { get; set; }
        }

        private class CreditReportFindHit
        {
            public DateTimeOffset RequestDate { get; set; }
            public int CreditReportId { get; set; }
        }

        private bool IsCivicRegNrToTestProviderDown(string civicRegNr)
        {
            if (NEnv.ClientCfg.Country.BaseCountry == "FI")
                return "200138-684K" == civicRegNr;
            else if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                return "199904181469" == civicRegNr;
            else
                throw new NotImplementedException();
        }

        public class GetCreditReportByIdResult
        {
            public int CreditReportId { get; set; }
            public DateTimeOffset RequestDate { get; set; }
            public int CustomerId { get; set; }
            public string ProviderName { get; set; }
            public IList<Item> Items { get; set; }
            public class Item
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public GetCreditReportByIdResult GetCreditReportById(int creditReportId, IList<string> requestedCreditReportFields, int currentUserId)
        {
            var result = Begin()
                    .PostJson("CreditReport/GetById",
                        new
                        {
                            creditReportId = creditReportId,
                            itemNames = requestedCreditReportFields.ToArray()
                        });
            if (result.IsNotFoundStatusCode)
                return null;
            else
                return result.ParseJsonAs<GetCreditReportByIdResult>();
        }

        public void RemoveCachedCreditReports(int customerId)
        {
            Begin()
                .PostJson("CreditReport/ClearCache",
                    new
                    {
                        customerId = customerId,
                    })
                .EnsureSuccessStatusCode();
        }

        public void RemoveCachedPersonInfo(int customerId)
        {
            Begin()
                .PostJson("PersonInfo/ClearCache",
                    new
                    {
                        customerId = customerId,
                    })
                .EnsureSuccessStatusCode();
        }
    }
}