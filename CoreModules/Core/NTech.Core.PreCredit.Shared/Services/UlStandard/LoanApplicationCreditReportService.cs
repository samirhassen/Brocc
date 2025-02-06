using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class LoanApplicationCreditReportService
    {
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ICreditReportService creditReportService;
        private readonly ICustomerClient customerClient;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IPreCreditContextFactoryService contextFactoryService;

        public LoanApplicationCreditReportService(INTechCurrentUserMetadata currentUser,
            ICreditReportService creditReportService, ICustomerClient customerClient, IPreCreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration, IPreCreditContextFactoryService contextFactoryService)
        {
            this.currentUser = currentUser;
            this.creditReportService = creditReportService;
            this.customerClient = customerClient;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
            this.contextFactoryService = contextFactoryService;
        }

        private static string[] customerCardCreditReportItemNames = new string[]
                {
                    "firstName",
                    "lastName",
                    "addressCountry",
                    "addressStreet",
                    "addressZipcode",
                    "addressCity"
                };

        public BuyNewLoanApplicationCreditReportResult BuyNew(string applicationNr, int customerId, ICivicRegNumber civicRegNr, List<string> requestedCreditReportFields, bool alsoUpdateCustomerCard)
        {
            var requestedFields = new HashSet<string>();
            if (requestedCreditReportFields != null)
                requestedFields.AddRange(requestedCreditReportFields);

            if (alsoUpdateCustomerCard)
            {
                requestedFields.AddRange(customerCardCreditReportItemNames);
            }

            var result = creditReportService.BuyStandardApplicationCreditReport(civicRegNr, customerId, requestedFields.ToList(), currentUser.UserId, envSettings.CreditReportProviderName, applicationNr);

            if (result.CreditReportId.HasValue)
            {
                if (alsoUpdateCustomerCard)
                    UpdateCustomerCardFromCreditReport(customerId, civicRegNr, result.Model);

                return new BuyNewLoanApplicationCreditReportResult
                {
                    CreditReportId = result.CreditReportId,
                    CreditReportItems = result.Model,
                    IsError = false
                };
            }
            else
            {
                return new BuyNewLoanApplicationCreditReportResult
                {
                    ErrorMessage = result.ErrorMessage,
                    IsError = true
                };
            }
        }

        public (PartialCreditReportModel Report, List<string> RequestedItems) FindExistingOrBuyNewCreditReport(int customerId, string applicationNr,
                bool isAllowedToBuyNewCreditReports, ICivicRegNumber civicRegNr, ICreditReportClient creditReportClient,
                bool alsoUpdateCustomerCard, ICoreClock clock)
        {
            var existingReports = creditReportClient.FindCreditReportsByReason("CreditApplication", applicationNr, false);

            var today = clock.Today;

            bool IsOldReport(FindForCustomerCreditReportModel report)
            {
                if (report.RequestDate.DateTime.Date == today)
                    return false;
                var ageInDays = today.Subtract(report.RequestDate.Date).TotalDays;
                return ageInDays > (envSettings.PersonCreditReportReuseDays - 1);
            }
            
            var reusableReport = (existingReports ?? new List<FindForCustomerCreditReportModel>())
                .Where(x => x.CustomerId == customerId && !IsOldReport(x))
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            var requestedFields = new HashSet<string> {
                "nrOfPaymentRemarks", "riskValue", "hasPosteRestanteAddress", "hasPostBoxAddress",
                "latestIncomePerYear", "hasGuardian", "personstatus", "hasKfmBalance",
                "hasSwedishSkuldsanering", "scoreValue", "hasDomesticAddress" };

            PartialCreditReportModel creditReportItems = null;
            if (reusableReport != null)
            {
                //A credit report for this customer, bought for this application today is reused. Otherwise a new one is aquired.
                //We dont want to reuse those from other applications since this causes issues when reasoning about when to archive things.
                if (alsoUpdateCustomerCard)
                {
                    requestedFields.AddRange(customerCardCreditReportItemNames);
                }
                var result = creditReportClient.GetCreditReportById(reusableReport.Id, requestedFields.ToList());
                creditReportItems = new PartialCreditReportModel(result
                                        .Items
                                        .Select(x => new PartialCreditReportModel.Item
                                        {
                                            Name = x.Name,
                                            Value = x.Value
                                        }).ToList());
            }
            else if (isAllowedToBuyNewCreditReports)
            {
                var result = BuyNew(applicationNr, customerId, civicRegNr, requestedFields.ToList(), alsoUpdateCustomerCard);
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    throw new NTechCoreWebserviceException(result.ErrorMessage) { ErrorCode = "failedToBuyCreditReport" };
                }
                creditReportItems = result?.CreditReportItems ?? new PartialCreditReportModel();
            }
            else
            {
                //Used when testing on a historical application or for using things like mainApplicantCreditReportRiskValue as pd but only when a report exists
                creditReportItems = new PartialCreditReportModel();
            }

            if(alsoUpdateCustomerCard)
                UpdateCustomerCardFromCreditReport(customerId, civicRegNr, creditReportItems);            

            return (Report: creditReportItems, RequestedItems: requestedFields.ToList());
        }

        private void UpdateCustomerCardFromCreditReport(int customerId, ICivicRegNumber civicRegNr, PartialCreditReportModel creditReportItems)
        {
            var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "firstName", "lastName", "addressZipcode").Opt(customerId);
            var customerPropertiesToUpdate = new List<CreateOrUpdatePersonRequest.Property>();
            void AddProperty(string name, string value)
            {
                if (value != null)
                    customerPropertiesToUpdate.Add(new CreateOrUpdatePersonRequest.Property
                    {
                        ForceUpdate = true,
                        Name = name,
                        Value = value
                    });
            }
            if (customerData.Opt("firstName") == null && customerData.Opt("lastName") == null)
            {
                AddProperty("firstName", creditReportItems.Get("firstName").StringValue.Optional);
                AddProperty("lastName", creditReportItems.Get("lastName").StringValue.Optional);
            }
            var creditReportZipcode = creditReportItems.Get("addressZipcode").StringValue.Optional;
            if (customerData.Opt("addressZipcode") == null && creditReportZipcode != null)
            {
                AddProperty("addressZipcode", creditReportZipcode);
                AddProperty("addressStreet", creditReportItems.Get("addressStreet").StringValue.Optional);
                AddProperty("addressCity", creditReportItems.Get("addressCity").StringValue.Optional);
                AddProperty("addressCountry", creditReportItems.Get("addressCountry").StringValue.Optional ?? clientConfiguration.Country.BaseCountry);
            }
            if (customerPropertiesToUpdate.Count > 0)
                customerClient.CreateOrUpdatePerson(new CreateOrUpdatePersonRequest
                {
                    CivicRegNr = civicRegNr.NormalizedValue,
                    EventType = "ApplicationStandardCreditReport",
                    Properties = customerPropertiesToUpdate,
                    ExpectedCustomerId = customerId
                }
            );
        }
    }

    public class BuyNewLoanApplicationCreditReportResult
    {
        public int? CreditReportId { get; set; }
        public PartialCreditReportModel CreditReportItems { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
    }
}