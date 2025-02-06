using nCustomer.Code.Services.Settings;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.Shared
{
    internal static class TestSettings
    {
        public static void UpdateSetting(string settingCode, Func<Dictionary<string, string>, Dictionary<string, string>> update, SupportShared support)
        {
            var currentValue = support
                .CreateCachedSettingsService()
                .LoadSettingsNoCache(settingCode);

            var newValue = update(currentValue);

            var settingService = new SettingsService(
                new Customer.Shared.Settings.SettingsModelSource(support.ClientConfiguration),
                support.CreateCustomerKeyValueStoreService(),
                support.CurrentUser, support.ClientConfiguration,
                (_, __) =>
                {
                    CachedSettingsService.ClearCache();
                });
            settingService.SaveSettingsValues(settingCode, newValue, (IsSystemUser: true, GroupMemberships: null));
        }
    }
}
