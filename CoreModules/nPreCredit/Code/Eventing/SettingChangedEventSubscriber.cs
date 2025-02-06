using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Threading;

namespace nPreCredit.Code
{
    public class SettingChangedEventSubscriber : EventSubscriberBase, IEventSubscriber
    {
        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            Subscribe(PreCreditEventCode.SettingChanged, OnSettingChanged, subscribe);
        }

        public void OnSettingChanged(string settingCode, CancellationToken ct)
        {
            Services.SharedStandard.LtlDataTables.OnSettingChanged(settingCode);
            CachedSettingsService.OnSettingChanged(settingCode);
        }
    }
}