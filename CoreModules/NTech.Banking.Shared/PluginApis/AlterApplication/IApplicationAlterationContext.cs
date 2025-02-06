using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;

namespace NTech.Banking.PluginApis.AlterApplication
{
    public interface IApplicationAlterationContext
    {
        DateTimeOffset Now { get; }
        int WorkflowVersion { get; }

        int CreateOrUpdateCompany(IOrganisationNumber orgnr, Dictionary<string, string> customerData, bool isTrustedSource, string applicationNr, int? expectedCustomerId = null);

        int CreateOrUpdatePerson(ICivicRegNumber civicRegNr, Dictionary<string, string> customerData, bool isTrustedSource, string applicationNr, int? expectedCustomerId = null, DateTime? birthDate = null);

        void SetKeyValueStoreValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null);

        string SerializeObject<T>(T value);

        IApplicationDataSourceResponse GetDataSourceItems(string applicationNr, Dictionary<string, HashSet<string>> itemNamesByDataSourceName);
    }
}