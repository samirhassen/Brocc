using Newtonsoft.Json;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using nTest.Code;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace nTest
{
    public class TimeMachine
    {
        private TimeMachine()
        {
        }

        private readonly ConcurrentDictionary<string, ServiceClient> serviceClients =
            new ConcurrentDictionary<string, ServiceClient>();

        private readonly object timeLock = new object();

        public DateTimeOffset SetTime(DateTimeOffset time, bool publishUpdateEvent)
        {
            lock (timeLock)
            {
                using (var tr = DbSingleton.SharedInstance.Db.BeginTransaction())
                {
                    var t = tr.Get<CurrentTimeItem>("v1", "currentTime");
                    if (t == null)
                    {
                        var newT = new CurrentTimeItem { CurrentTime = time };
                        tr.AddOrUpdate("v1", "currentTime", newT);
                        tr.Commit();
                        if (publishUpdateEvent)
                        {
                            PublishTimeChangeEvent(newT.CurrentTime);
                        }

                        return newT.CurrentTime;
                    }

                    if (t.CurrentTime == time) return t.CurrentTime;

                    t.CurrentTime = time;
                    tr.AddOrUpdate("v1", "currentTime", t);
                    tr.Commit();
                    if (publishUpdateEvent)
                    {
                        PublishTimeChangeEvent(t.CurrentTime);
                    }

                    return t.CurrentTime;
                }
            }
        }

        private static List<string> TimeMachineSupportingServiceNames = new List<string>
        {
            "nScheduler",
            "nCredit",
            "nCreditReport",
            "nPreCredit",
            "nSavings",
            "nCustomer",
            "nCustomerPages",
            "nBackOffice",
            "NTechHost"
        };

        private void PublishTimeChangeEvent(DateTimeOffset time)
        {
            var s = NEnv.ServiceRegistry;

            foreach (var serviceName in TimeMachineSupportingServiceNames.Where(serviceName =>
                         s.ContainsService(serviceName)))
            {
                try
                {
                    var serviceClient = serviceClients.GetOrAdd(
                        serviceName,
                        _ => LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry)
                            .CreateClient(LegacyHttpServiceSystemUser.SharedInstance, serviceName));
                    serviceClient.ToSync(() => serviceClient.CallVoid(
                        x => x.PostJson(GetSetTimeUrl(serviceName), new { now = time }),
                        x => x.EnsureSuccessStatusCode()));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Set-TimeMachine-Time failed for '{serviceName}'", ex);
                }
            }

            NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent("TimeMachineTimeChanged",
                JsonConvert.SerializeObject(new { currentTime = time }));
        }

        private static string GetSetTimeUrl(string serviceName) =>
            serviceName == "NTechHost" ? "Api/Set-TimeMachine-Time" : "Set-TimeMachine-Time";

        private class CurrentTimeItem
        {
            public int Id { get; set; }
            public DateTimeOffset CurrentTime { get; set; }
        }

        public void Init()
        {
            using (var tr = DbSingleton.SharedInstance.Db.BeginTransaction())
            {
                var t = tr.Get<CurrentTimeItem>("v1", "currentTime");
                if (t == null)
                {
                    tr.AddOrUpdate("v1", "currentTime", new CurrentTimeItem { CurrentTime = NEnv.DefaultTime });
                    tr.Commit();
                }
            }
        }

        public DateTimeOffset GetCurrentTime()
        {
            using (var tr = DbSingleton.SharedInstance.Db.BeginTransaction())
            {
                var t = tr.Get<CurrentTimeItem>("v1", "currentTime");
                if (t == null)
                {
                    throw new Exception("Missing current time");
                }
                else
                {
                    return t.CurrentTime;
                }
            }
        }

        private static readonly object sharedInstanceLock = new object();
        private static TimeMachine sharedInstance = null;

        public static TimeMachine SharedInstance
        {
            get
            {
                if (sharedInstance == null)
                {
                    lock (sharedInstanceLock)
                    {
                        if (sharedInstance == null)
                        {
                            var t = new TimeMachine();
                            sharedInstance = t;
                        }
                    }
                }

                return sharedInstance;
            }
        }
    }
}