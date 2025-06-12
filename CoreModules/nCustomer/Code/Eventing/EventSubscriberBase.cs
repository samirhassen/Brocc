using IdentityModel.Client;
using NTech.Core.Customer.Shared.Services;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace nCustomer.Code
{
    public abstract class EventSubscriberBase
    {
        private static Lazy<ConcurrentStack<string>> subIds = new Lazy<ConcurrentStack<string>>(() => new ConcurrentStack<string>());

        public void OnShutdown(Action<string> unsubscribe)
        {
            string subId;
            while (subIds.Value.TryPop(out subId))
            {
                unsubscribe(subId);
            }
        }

        protected void Subscribe(CustomerEventCode evt, Action<string, CancellationToken> onEvt, Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            var subId = subscribe(
                evt.ToString(),
                (data, ct) => onEvt(data, ct));
            subIds.Value.Push(subId);
        }

        public static string AquireBearerToken()
        {
            return NTechCache.WithCache("nCustomerEventAutomation.2923423a55a-9a86-44af-a230-b504b3749164", TimeSpan.FromMinutes(3), () =>
            {
                var tokenClient = new TokenClient(
                                        new Uri(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), "id/connect/token").ToString(),
                                        "nTechSystemUser",
                                        "nTechSystemUser");

                var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
                var token = tokenClient.RequestResourceOwnerPasswordAsync(credentials.Item1, credentials.Item2, scope: "nTech1").Result;

                if (token.IsError)
                {
                    throw new Exception("Bearer token login failed in nCustomer event automation :" + token.Error);
                }

                return token.AccessToken;
            });
        }

        private static List<string> CrossServiceEventServiceNames = new List<string>
        {
            "nCredit",
            "nPreCredit",
            "nSavings",
            "nBackOffice",
            "NTechHost"
        };

        public static void BroadcastCrossServiceEvent(string eventName, string data)
        {
            var requestData = new
            {
                eventSource = "nCustomer",
                eventName = eventName,
                eventData = data
            };
            var sr = NEnv.ServiceRegistry;
            foreach (var serviceName in CrossServiceEventServiceNames)
            {
                if (!sr.ContainsService(serviceName))
                    continue;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.SetBearerToken(AquireBearerToken());
                client.BaseAddress = new Uri(sr.Internal[serviceName]);
                try
                {
                    var response = client.PostAsJsonAsync("Api/Common/ReceiveEvent", requestData).Result;
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    NLog.Error(ex, "BroadcastCrossServiceEvent failed");
                }
            }
        }

        public static ICrossServiceEventService SharedService { get; } = new CrossServiceEventServiceImpl();

        private class CrossServiceEventServiceImpl : ICrossServiceEventService
        {
            public void BroadcastCrossServiceEvent(string eventName, string data) => EventSubscriberBase.BroadcastCrossServiceEvent(eventName, data);
        }
    }
}