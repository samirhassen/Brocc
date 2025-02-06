using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nTest.Controllers
{
    public class CreditDriverSchedulerClient
    {
        public void TriggerTimeslot(string name, IDictionary<string, string> schedulerData = null)
        {
            NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nScheduler"), NEnv.AutomationBearerToken(), timeout: TimeSpan.FromHours(3))
                .PostJson("Api/TriggerTimeslot", new
                {
                    name = name,
                    schedulerData = schedulerData
                }, headers: new Dictionary<string, string> { { "x-ntech-timetravel-time", TimeMachine.SharedInstance.GetCurrentTime().ToString("o") } })
                .EnsureSuccessStatusCode();
        }
    }
}