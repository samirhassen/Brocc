using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static nPreCredit.Code.Services.CreditReportService;

namespace nPreCredit.Code.Services
{
    public class CreditReportService : ICreditReportClient, ICreditReportService
    {
        private readonly ServiceClientFactory serviceClientFactory;
        private readonly INHttpServiceUser serviceUser;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;

        public CreditReportService(ServiceClientFactory serviceClientFactory, INHttpServiceUser serviceUser, IPreCreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration)
        {
            this.serviceClientFactory = serviceClientFactory;
            this.serviceUser = serviceUser;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
        }

        public PartialCreditReportModelAndStatus BuyCreditReport(ICivicRegNumber civicRegNr, int customerId, IList<string> requestedCreditReportFields, string providerName, bool forceBuyNew, bool returnFieldsOnNew, string reasonType, string reasonData, Dictionary<string, string> additionalParameters)
        {
            if (!envSettings.IsProduction)
            {
                if (IsCivicRegNrToTestProviderDown(civicRegNr.NormalizedValue))
                {
                    return new PartialCreditReportModelAndStatus
                    {
                        ProviderIsDown = true
                    };
                }
            }

            var creditReportClient = CreateCreditReportClient();

            var reports = PostJsonToCreditReport<List<CreditReportFindHit>>(
                "CreditReport/Find", new { providerName = providerName, customerId = customerId }, creditReportClient);

            var latestReport = reports.OrderByDescending(x => x.RequestDate).FirstOrDefault();
            int? creditReportId = null;
            List<GetCreditReportByIdResult.Item> creditReportItems = null;
            bool isNew = false;

            if (latestReport == null || forceBuyNew || IsConsideredOldReport(latestReport))
            {
                var result = CatchingTimeout(() =>
                {
                    var r = PostJsonToCreditReport<CreditReportBuyNewResult>("CreditReport/BuyNew", new
                    {
                        providerName = providerName,
                        civicRegNr = civicRegNr.NormalizedValue,
                        customerId = customerId,
                        returningItemNames = returnFieldsOnNew ? requestedCreditReportFields?.ToArray() : null,
                        additionalParameters,
                        reasonType = reasonType,
                        reasonData = reasonData,
                    }, creditReportClient, timeout: TimeSpan.FromSeconds(15));
                    isNew = true;
                    return r;
                }, () => null);

                if (result == null)
                    return new PartialCreditReportModelAndStatus
                    {
                        ProviderIsDown = true
                    };
                else if (result.IsInvalidCredentialsError || result.IsTimeoutError)
                    return new PartialCreditReportModelAndStatus
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
                var creditReport = GetCreditReportById(latestReport.CreditReportId, requestedCreditReportFields);
                creditReportItems = creditReport.Items;
            }

            return new PartialCreditReportModelAndStatus
            {
                CreditReportId = creditReportId,
                Model = new PartialCreditReportModel(creditReportItems == null ? null : creditReportItems
                    .Select(x => new PartialCreditReportModel.Item
                    {
                        Name = x.Name,
                        Value = x.Value
                    }).ToList()),
                IsNewReport = isNew,
                ProviderIsDown = false,
                IsInvalidCredentialsError = false
            };
        }

        public PartialCreditReportModelAndStatus BuyStandardApplicationCreditReport(ICivicRegNumber civicRegNr, int customerId, IList<string> requestedCreditReportFields, int currentUserId, string providerName, string applicationNr)
        {
            var creditReportClient = CreateCreditReportClient();

            var result = CatchingTimeout(() =>
            {
                var r = PostJsonToCreditReport<CreditReportBuyNewResult>("CreditReport/BuyNew",
                        new
                        {
                            providerName = providerName,
                            civicRegNr = civicRegNr.NormalizedValue,
                            customerId = customerId,
                            returningItemNames = requestedCreditReportFields?.ToArray(),
                            reasonType = "CreditApplication",
                            reasonData = applicationNr
                        }, creditReportClient, timeout: TimeSpan.FromSeconds(15));
                return r;
            }, () => null);

            if (result == null)
                return new PartialCreditReportModelAndStatus
                {
                    ProviderIsDown = true
                };
            else if (result.IsInvalidCredentialsError || result.IsTimeoutError)
                return new PartialCreditReportModelAndStatus
                {
                    IsInvalidCredentialsError = result.IsInvalidCredentialsError,
                    ProviderIsDown = result.IsTimeoutError
                };
            else
            {
                return new PartialCreditReportModelAndStatus
                {
                    CreditReportId = result.CreditReportId,
                    Model = new PartialCreditReportModel(result.Items
                        .Select(x => new PartialCreditReportModel.Item
                        {
                            Name = x.Name,
                            Value = x.Value
                        }).ToList()),
                    IsNewReport = true,
                    ProviderIsDown = false,
                    IsInvalidCredentialsError = false
                };
            }
        }

        public PartialCreditReportModelAndStatus BuyCompanyCreditReport(IOrganisationNumber orgnr, int customerId, IList<string> requestedCreditReportFields,
            string providerName, bool forceBuyNew, bool returnFieldsOnNew, (string type, string data) reason, Dictionary<string, string> additionalParameters)
        {
            var creditReportClient = CreateCreditReportClient();

            var reports = PostJsonToCreditReport<List<CreditReportFindHit>>("CompanyCreditReport/Find",
                new { providerName = providerName, customerId = customerId }, creditReportClient);

            var latestReport = reports.OrderByDescending(x => x.CreditReportId).FirstOrDefault();
            int? creditReportId = null;
            List<GetCreditReportByIdResult.Item> creditReportItems = null;
            bool isNew = false;
            if (latestReport == null || forceBuyNew || IsConsideredOldCompanyReport(latestReport))
            {
                var result = PostJsonToCreditReport<CreditReportBuyNewResult>("CompanyCreditReport/BuyNew",
                    new
                    {
                        providerName = providerName,
                        orgnr = orgnr.NormalizedValue,
                        customerId = customerId,
                        returningItemNames = returnFieldsOnNew ? requestedCreditReportFields?.ToArray() : null,
                        additionalParameters,
                        reasonType = reason.type,
                        reasonData = reason.data
                    }, creditReportClient, timeout: TimeSpan.FromSeconds(60));
                isNew = true;

                if (result == null)
                    return new PartialCreditReportModelAndStatus
                    {
                        ProviderIsDown = true
                    };
                else if (result.IsInvalidCredentialsError || result.IsTimeoutError)
                    return new PartialCreditReportModelAndStatus
                    {
                        IsInvalidCredentialsError = result.IsInvalidCredentialsError,
                        ProviderIsDown = result.IsTimeoutError
                    };
                else if (!result.Success)
                {
                    return new PartialCreditReportModelAndStatus
                    {
                        ErrorMessage = result.ErrorMessage
                    };
                }
                else
                {
                    creditReportId = result.CreditReportId;
                    creditReportItems = result.Items;
                }
            }
            else
            {
                creditReportId = latestReport.CreditReportId;
                creditReportItems = PostJsonToCreditReport<CreditReportResult>("CompanyCreditReport/GetById", new
                {
                    creditReportId = creditReportId.Value,
                    itemNames = requestedCreditReportFields.ToArray()
                }, creditReportClient).Items;
            }

            return new PartialCreditReportModelAndStatus
            {
                CreditReportId = creditReportId,
                Model = new PartialCreditReportModel(creditReportItems == null ? null : creditReportItems
                    .Select(x => new PartialCreditReportModel.Item
                    {
                        Name = x.Name,
                        Value = x.Value
                    }).ToList()),
                IsNewReport = isNew,
                ProviderIsDown = false,
                IsInvalidCredentialsError = false
            };
        }

        public GetCreditReportByIdResult GetCreditReportById(int creditReportId, IList<string> requestedCreditReportFields)
        {
            var creditReportClient = CreateCreditReportClient();
            return GetCreditReportById(creditReportClient, creditReportId, requestedCreditReportFields);
        }

        public List<FindForCustomerCreditReportModel> FindCreditReportsByReason(string reasonType, string reasonData, bool findCompanyReports)
        {
            var creditReportClient = CreateCreditReportClient();
            return creditReportClient.ToSync(() => creditReportClient.Call(
                x => x.PostJson("Api/CreditReports/FindByReason", new
                {
                    reasonType,
                    reasonData,
                    findCompanyReports
                }),
                x => x.ParseJsonAsAnonymousType(new { CreditReports = (List<FindForCustomerCreditReportModel>)null }))).CreditReports;
        }

        private GetCreditReportByIdResult GetCreditReportById(ServiceClient creditReportClient, int creditReportId, IList<string> requestedCreditReportFields) =>
            creditReportClient.ToSync(() => creditReportClient.Call(
                x => x.PostJson("CreditReport/GetById", new
                {
                    creditReportId = creditReportId,
                    itemNames = requestedCreditReportFields.ToArray()
                }),
                x =>
                {
                    if(x.IsNotFoundStatusCode)
                        throw new NTechCoreWebserviceException("No such creditreport exists") { ErrorCode = "noSuchCreditReportExists" };
                    else
                        return x.ParseJsonAs<GetCreditReportByIdResult>();
                }));

        private TResult PostJsonToCreditReport<TResult>(string relativeUrl, object request, ServiceClient creditReportClient, TimeSpan? timeout = null) =>
            creditReportClient.ToSync(() => creditReportClient.Call(
                x => x.PostJson(relativeUrl, request),
                x => x.ParseJsonAs<TResult>(), timeout: timeout));

        private ServiceClient CreateCreditReportClient()
        {
            return serviceClientFactory.CreateClient(serviceUser, "nCreditReport");
        }

        private bool IsConsideredOldReport(CreditReportFindHit report)
        {
            if (report == null)
                return false;
            return report.AgeInDays > (envSettings.PersonCreditReportReuseDays - 1);
        }

        private bool IsConsideredOldCompanyReport(CreditReportFindHit report)
        {
            if (report == null)
                return false;
            return report.AgeInDays > (envSettings.CompanyCreditReportReuseDays - 1);
        }

        public class PartialCreditReportModelAndStatus
        {
            public int? CreditReportId { get; set; }
            public PartialCreditReportModel Model { get; set; }
            public bool ProviderIsDown { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public bool IsNewReport { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class CreditReportBuyNewResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public int CreditReportId { get; set; }
            public List<GetCreditReportByIdResult.Item> Items { get; set; }
            public bool IsTimeoutError { get; set; }
        }

        private class CreditReportResult
        {
            public DateTimeOffset RequestDate { get; set; }
            public int CreditReportId { get; set; }
            public List<GetCreditReportByIdResult.Item> Items { get; set; }
        }

        private class CreditReportFindHit
        {
            public DateTimeOffset RequestDate { get; set; }
            public int CreditReportId { get; set; }
            public int? AgeInDays { get; set; }
        }

        private bool IsCivicRegNrToTestProviderDown(string civicRegNr)
        {
            if (clientConfiguration.Country.BaseCountry == "FI")
                return "200138-684K" == civicRegNr;
            else if (clientConfiguration.Country.BaseCountry == "SE")
                return "199904181469" == civicRegNr;
            else
                throw new NotImplementedException();
        }

        private static T CatchingTimeout<T>(Func<T> f, Func<T> onTimeout)
        {
            try
            {
                return f();
            }
            catch (AggregateException e)
            {
                if (e.InnerException != null && (e.InnerException as TaskCanceledException) != null)
                    return onTimeout();
                else
                    throw;
            }
            catch (TaskCanceledException)
            {
                return onTimeout();
            }
        }
    }

    public interface ICreditReportService
    {
        PartialCreditReportModelAndStatus BuyStandardApplicationCreditReport(ICivicRegNumber civicRegNr, int customerId, IList<string> requestedCreditReportFields, int currentUserId, string providerName, string applicationNr);
    }
}