using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Threading;

namespace nCredit.Code
{
    public class SettingChangedEventSubscriber : EventSubscriberBase, IEventSubscriber
    {
        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            Subscribe(CreditEventCode.SettingChanged, OnSettingChanged, subscribe);
        }

        public void OnSettingChanged(string data, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(data))
                CachedSettingsService.OnSettingChanged(data);
        }
    }
}