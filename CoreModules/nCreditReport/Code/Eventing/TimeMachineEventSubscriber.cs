using Newtonsoft.Json;
using NTech;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Threading;

namespace nCreditReport.Code
{
    public class TimeMachineEventSubscriber : EventSubscriberBase, IEventSubscriber
    {
        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            if (NEnv.IsProduction)
                return;
            Subscribe(CreditReportEventCode.TimeMachineTimeChanged, OnTimeMachineTimeChanged, subscribe);
        }

        public void OnTimeMachineTimeChanged(string data, CancellationToken ct)
        {
            var d = JsonConvert.DeserializeAnonymousType(data, new { currentTime = (DateTimeOffset?)null });
            if (d?.currentTime != null && d.currentTime.HasValue)
            {
                ClockFactory.TrySetApplicationDateAndTime(d.currentTime.Value);
            }
        }
    }
}