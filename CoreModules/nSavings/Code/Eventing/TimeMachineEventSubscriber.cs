using System;
using System.Threading;
using Newtonsoft.Json;
using NTech;
using NTech.Services.Infrastructure.Eventing;

namespace nSavings.Code.Eventing
{
    public class TimeMachineEventSubscriber : EventSubscriberBase, IEventSubscriber
    {
        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            if (NEnv.IsProduction)
                return;
            Subscribe(SavingsEventCode.TimeMachineTimeChanged, OnTimeMachineTimeChanged, subscribe);
        }

        public static void OnTimeMachineTimeChanged(string data, CancellationToken ct)
        {
            var d = JsonConvert.DeserializeAnonymousType(data, new { currentTime = (DateTimeOffset?)null });
            if (d?.currentTime != null)
            {
                ClockFactory.TrySetApplicationDateAndTime(d.currentTime.Value);
            }
        }
    }
}