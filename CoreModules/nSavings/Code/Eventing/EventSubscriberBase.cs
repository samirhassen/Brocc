using NTech.Services.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace nSavings.Code
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

        protected void Subscribe(SavingsEventCode evt, Action<string, CancellationToken> onEvt, Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            var subId = subscribe(
                evt.ToString(),
                (data, ct) => onEvt(data, ct));
            subIds.Value.Push(subId);
        }

        protected NHttp.NHttpCall BeginCallSelf()
        {
            var token = AquireBearerToken();
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nSavings"]), token);
        }

        public static string AquireBearerToken()
        {
            return NTechCache.WithCache("nSavingsAutomation.29f2a55a-9a86-44af-a230-b504b3749164", TimeSpan.FromMinutes(3), () =>
            {
                var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
                return NHttp.AquireSystemUserAccessTokenWithUsernamePassword(credentials.Item1, credentials.Item2, new Uri(NEnv.ServiceRegistry.Internal["nUser"]));
            });
        }
    }
}