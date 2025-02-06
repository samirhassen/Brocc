using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Services
{
    public class CachedSettingsService
    {
        private readonly Lazy<ICustomerClientLoadSettingsOnly> customerClient;
        private static readonly ConcurrentDictionary<string, CacheItem> settingsCache = new ConcurrentDictionary<string, CacheItem>();
        private static TimeSpan cacheDuration = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<string, Action<string>> settingChangeCallbacks = new ConcurrentDictionary<string, Action<string>>();

        public CachedSettingsService(Func<ICustomerClientLoadSettingsOnly> createCustomerClient)
        {
            this.customerClient = new Lazy<ICustomerClientLoadSettingsOnly>(createCustomerClient);
        }

        public CachedSettingsService(ICustomerClientLoadSettingsOnly customerClient)
        {
            this.customerClient = new Lazy<ICustomerClientLoadSettingsOnly>(() => customerClient);
        }

        public Func<DateTimeOffset> getUtcNow = null; //To allow testing
        private DateTimeOffset UtcNow => getUtcNow == null ? DateTimeOffset.UtcNow : getUtcNow();

        public Dictionary<string, string> LoadSettings(string settingCode)
        {
            if (settingsCache.TryGetValue(settingCode, out var cacheItem))
            {
                if (UtcNow.Subtract(cacheItem.creationDate) <= cacheDuration)
                    return cacheItem.data;
            }
            return LoadSettingsNoCache(settingCode);
        }

        public Dictionary<string, string> LoadSettings(string settingCode, bool forceReload) => forceReload ? LoadSettingsNoCache(settingCode) : LoadSettings(settingCode);

        public Dictionary<string, string> LoadSettingsNoCache(string settingCode)
        {
            var result = customerClient.Value.LoadSettings(settingCode);
            var cacheItem = new CacheItem
            {
                data = result,
                creationDate = UtcNow
            };
            settingsCache.AddOrUpdate(settingCode, cacheItem, (_, __) => cacheItem);
            return cacheItem.data;
        }

        public static void OnSettingChanged(string settingCode)
        {
            if (string.IsNullOrWhiteSpace(settingCode))
                return;
            settingsCache.TryRemove(settingCode, out var _);

            foreach (var callback in settingChangeCallbacks.Values)
                callback?.Invoke(settingCode);

        }

        public static string RegisterSettingChangedCallback(Action<string> callback)
        {
            var key = Guid.NewGuid().ToString();
            settingChangeCallbacks.AddOrUpdate(key, _ => callback, (_, __) => callback);
            return key;
        }

        public static void RemoveSettingsChangedCallback(string callbackId) => settingChangeCallbacks.TryRemove(callbackId, out _);

        public static void ClearCache()
        {
            settingsCache.Clear();
        }

        private class CacheItem
        {
            public Dictionary<string, string> data;
            public DateTimeOffset creationDate;
        }
    }
}
