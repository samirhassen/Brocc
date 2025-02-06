using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;

namespace NTech.Banking.PluginApis.CreateApplication
{
    public interface IApplicationCreationContext
    {
        DateTimeOffset Now { get; }
        int WorkflowVersion { get; }

        string GenerateNewApplicationNr();

        int CreateOrUpdateCompany(IOrganisationNumber orgnr, Dictionary<string, string> customerData, bool isTrustedSource, string applicationNr, int? expectedCustomerId = null);

        int CreateOrUpdatePerson(ICivicRegNumber civicRegNr, Dictionary<string, string> customerData, bool isTrustedSource, string applicationNr, int? expectedCustomerId = null, DateTime? birthDate = null);

        void SetKeyValueStoreValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null);

        string SerializeObject<T>(T value);

        List<IApplicationAffiliateModel> GetAffiliates();
    }

    public interface IApplicationAffiliateModel
    {
        string ProviderName { get; }
        string ProviderToken { get; }
        bool IsSelf { get; }

        T GetCustomPropertyAnonymous<T>(T templateObject);
    }
}