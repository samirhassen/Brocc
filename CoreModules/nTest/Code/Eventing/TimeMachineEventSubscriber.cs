using Duende.IdentityModel.Client;
using NTech.Services.Infrastructure.Eventing;
using nTest.Code;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace nTest
{
    public class TimeMachineEventSubscriber : EventSubscriberBase, IEventSubscriber
    {
        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            Subscribe(nTestEventCode.TimeMachineTimeChanged, OnTimeMachineTimeChanged, subscribe);
        }

        public void OnTimeMachineTimeChanged(string data, CancellationToken ct)
        {
            try
            {
                BroadcastCrossServiceEvent(nTestEventCode.TimeMachineTimeChanged.ToString(), data, "nCredit", "nSavings", "nPreCredit", "nCustomer", "nCreditReport", "NTechHost");
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error in OnTimeMachineTimeChanged");
            }
        }

        private void BroadcastCrossServiceEvent(string eventName, string data, params string[] serviceNames)
        {
            foreach (var serviceName in serviceNames)
            {
                if (NEnv.ServiceRegistry.ContainsService(serviceName))
                {
                    BroadcastCrossServiceEvent(eventName, data, serviceName);
                }
            }
        }

        private void BroadcastCrossServiceEvent(string eventName, string data, string serviceName)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal[serviceName]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.SetBearerToken(NEnv.AutomationBearerToken());
            var response = client.PostAsJsonAsync("Api/Common/ReceiveEvent", new
            {
                eventSource = "nTest",
                eventName = eventName,
                eventData = data
            }).Result;
            response.EnsureSuccessStatusCode();
        }
    }
}