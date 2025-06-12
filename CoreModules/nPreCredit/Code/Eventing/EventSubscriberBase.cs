using IdentityModel.Client;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace nPreCredit.Code
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

        protected void Subscribe(PreCreditEventCode evt, Action<string, CancellationToken> onEvt, Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            var subId = subscribe(
                evt.ToString(),
                (data, ct) => onEvt(data, ct));
            subIds.Value.Push(subId);
        }

        protected INTechCurrentUserMetadata GetUser()
        {
            return new NoUser();
        }

        private class NoUser : INTechCurrentUserMetadata
        {
            public bool ContextHasUser => false;

            public int UserId => default(int);

            public string InformationMetadata => null;

            public bool IsSystemUser => false;

            public string AuthenticationLevel => null;

            public string AccessToken => null;
            public string ProviderName => null;
            public bool IsProvider => false;
        }

        public static string AquireBearerToken()
        {
            return NTechCache.WithCache("nPreCreditEventAutomation.29f2a55a-9a86-44af-a230-b504b3749164", TimeSpan.FromMinutes(3), () =>
            {
                var tokenClient = new TokenClient(
                                        new Uri(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), "id/connect/token").ToString(),
                                        "nTechSystemUser",
                                        "nTechSystemUser");

                var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
                var token = tokenClient.RequestResourceOwnerPasswordAsync(credentials.Item1, credentials.Item2, scope: "nTech1").Result;

                if (token.IsError)
                {
                    throw new Exception("Bearer token login failed in nPreCredit event automation :" + token.Error);
                }

                return token.AccessToken;
            });
        }
    }
}