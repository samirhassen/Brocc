using System;
using System.Collections.Generic;

namespace NTech.Core.Savings.Shared.Services
{
    public interface IKeyValueStoreService
    {
        void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null);

        string GetValue(string key, string keySpace);

        Dictionary<string, string> GetValues(ISet<string> keys, string keySpace);

        void SetValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null);

        T SetConcurrent<T>(string key, string keySpace, Func<T> createNew, Func<T, T> mergeOnExists) where T : class;
    }

    public enum KeyValueStoreKeySpaceCode
    {
        SavingsExternalVariablesV1,
        SavingsManualPaymentsV1,
        FinnishCustomsLatestExportAccountV1,
        FinnishCustomsLatestExportCustomersV1
    }
}