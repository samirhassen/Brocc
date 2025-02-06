using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Datasources
{
    public interface IApplicationDataSource
    {
        string DataSourceName { get; }

        Dictionary<string, string> GetItems(string applicationNr,
            ISet<string> names, ApplicationDataSourceMissingItemStrategy missingItemStrategy,
            Action<string> observeMissingItems = null,
            Func<string, string> getDefaultValue = null,
            Action<string> observeChangedItems = null);

        int? SetData(string applicationNr, string compoundItemName,
            bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue,
            INTechCurrentUserMetadata currentUser);

        bool IsSetDataSupported { get; }
    }

    public enum ApplicationDataSourceMissingItemStrategy
    {
        ThrowException,
        Skip,
        UseDefaultValue
    }
}