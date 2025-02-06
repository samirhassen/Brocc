using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CustomerOfficialDataService : ICustomerOfficialDataService
    {
        private readonly ApplicationDataSourceService applicationDataSourceService;

        public CustomerOfficialDataService(ApplicationDataSourceService applicationDataSourceService)
        {
            this.applicationDataSourceService = applicationDataSourceService;
        }

        public InitialCustomerInfo GetInitialCustomerInfo(string applicationNr, int applicantNr, NTechNavigationTarget back)
        {
            return GetInitialCustomerInfoByItemName(applicationNr, $"applicant{applicantNr}.customerId", $"applicant{applicantNr}.birthDate", back);
        }

        public InitialCustomerInfo GetInitialCustomerInfoByItemName(string applicationNr, string customerIdApplicationItemCompoundName, string customerBirthDateApplicationItemCompoundName, NTechNavigationTarget back)
        {
            var result = this.applicationDataSourceService.GetData(applicationNr, new ApplicationDataSourceServiceRequest
            {
                DataSourceName = Datasources.CreditApplicationItemDataSource.DataSourceNameShared,
                MissingItemStrategy = Datasources.ApplicationDataSourceMissingItemStrategy.Skip,
                Names = new List<string> { customerIdApplicationItemCompoundName, customerBirthDateApplicationItemCompoundName }.Where(x => x != null).ToHashSet()
            });
            var d = result.Values.Single();
            var customerIdRaw = d.Opt(customerIdApplicationItemCompoundName);
            var applicationBirthDate = customerBirthDateApplicationItemCompoundName == null ? null : d.Opt(customerBirthDateApplicationItemCompoundName);

            if (string.IsNullOrWhiteSpace(customerIdRaw))
                throw new ServiceException($"Missing required application item {customerIdApplicationItemCompoundName}") { IsUserSafeException = true, ErrorCode = "missingApplicationItem" };

            int customerId = int.Parse(customerIdRaw);
            return GetInitialCustomerInfoByCustomerIdI(customerId, applicationBirthDate, back);
        }

        public InitialCustomerInfo GetInitialCustomerInfoByCustomerId(int customerId, NTechNavigationTarget back)
        {
            return GetInitialCustomerInfoByCustomerIdI(customerId, null, back);
        }

        private InitialCustomerInfo GetInitialCustomerInfoByCustomerIdI(int customerId, string birthDateOverride, NTechNavigationTarget back)
        {
            var customerClient = new PreCreditCustomerClient();
            var officialData = customerClient.GetCustomerCardItems(customerId, "firstName", "addressZipcode", "email", "sanction", "birthDate", "wasOnboardedExternally", "includeInFatcaExport", "companyName", "isCompany", "localIsPep", "localIsSanction");

            return new InitialCustomerInfo
            {
                customerId = customerId,
                firstName = officialData?.Opt("firstName"),
                isSanctionRejected = officialData?.Opt("sanction") == "true",
                wasOnboardedExternally = officialData?.Opt("wasOnboardedExternally") == "true",
                includeInFatcaExport = ParseTriStateBoolean(officialData?.Opt("includeInFatcaExport")),
                customerCardUrl = PreCreditCustomerClient.GetCustomerCardUrl(customerId, back),
                legacyCustomerCardUrl = PreCreditCustomerClient.GetCustomerCardUrl(customerId, back, forceLegacyUi: true),
                customerFatcaCrsUrl = PreCreditCustomerClient.GetCustomerFatcaCrsUrl(customerId, back),
                birthDate = officialData?.Opt("birthDate") ?? birthDateOverride,
                isMissingAddress = officialData?.Opt("addressZipcode") == null,
                isMissingEmail = officialData?.Opt("email") == null,
                isCompany = officialData?.Opt("isCompany") == "true",
                companyName = officialData?.Opt("companyName"),
                pepKycCustomerUrl = PreCreditCustomerClient.GetCustomerPepKycUrl(customerId, back),
                localIsPep = ParseTriStateBoolean(officialData?.Opt("localIsPep")),
                localIsSanction = ParseTriStateBoolean(officialData?.Opt("localIsSanction"))
            };

        }

        private bool? ParseTriStateBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value == "true";
        }
    }

    public class InitialCustomerInfo
    {
        public string firstName { get; set; }
        public string birthDate { get; set; }
        public int customerId { get; set; }
        public bool isSanctionRejected { get; set; }
        public bool wasOnboardedExternally { get; set; }
        public bool? includeInFatcaExport { get; set; }
        public string customerCardUrl { get; set; }
        public string legacyCustomerCardUrl { get; set; }
        public string customerFatcaCrsUrl { get; set; }
        public bool isMissingAddress { get; set; }
        public bool isMissingEmail { get; set; }
        public bool isCompany { get; set; }
        public string companyName { get; set; }
        public string pepKycCustomerUrl { get; set; }
        public bool? localIsPep { get; set; }
        public bool? localIsSanction { get; set; }
    }

    public interface ICustomerOfficialDataService
    {
        InitialCustomerInfo GetInitialCustomerInfo(string applicationNr, int applicantNr, NTechNavigationTarget back);

        InitialCustomerInfo GetInitialCustomerInfoByItemName(string applicationNr, string customerIdApplicationItemCompoundName, string customerBirthDateApplicationItemCompoundName, NTechNavigationTarget back);

        InitialCustomerInfo GetInitialCustomerInfoByCustomerId(int customerId, NTechNavigationTarget back);
    }
}