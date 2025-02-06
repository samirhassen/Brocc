using Newtonsoft.Json;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace nScheduler.Code
{
    public class SchedulerTelemetryLogger : IEventSubscriber
    {
        private List<string> subscriberIds = new List<string>();

        public void OnShutdown(Action<string> unsubscribe)
        {
            foreach (var id in subscriberIds)
            {
                unsubscribe(id);
            }
        }

        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            if (NEnv.IsTelemetryLoggingEnabled)
            {
                subscriberIds.Add(subscribe(SchedulerEventCode.TriggerTimeSlotCompleted.ToString(), OnTriggerTimeSlotCompleted));
            }
        }

        private static Lazy<NTechSelfRefreshingBearerToken> telemetryUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
        {
            var user = NEnv.AutomationUser;
            return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistryNormal, Tuple.Create(user.Username, user.Password));
        });
        private void OnTriggerTimeSlotCompleted(string data, CancellationToken ct)
        {
            var d = JsonConvert.DeserializeAnonymousType(data, new
            {
                timeslotName = "",
                serviceRuns = (IList<ServiceRun>)null
            });

            var user = new LegacyHttpServiceBearerTokenUser(telemetryUser);
            var auditClient = LegacyServiceClientFactory.CreateAuditClient(user, NEnv.ServiceRegistryNormal);
            auditClient.LogTelemetryData("SchedulerData", JsonConvert.SerializeObject(
                new
                {
                    timeslotName = d.timeslotName,
                    date = DateTimeOffset.Now,
                    serviceRuns = d.serviceRuns.Select(x => new
                    {
                        x.JobName,
                        x.StartDate,
                        x.EndStatus,
                        x.RuntimeInMs
                    }).ToList()
                }));
        }
    }
}