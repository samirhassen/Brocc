using System;
using System.Collections.Concurrent;
using System.Threading;
using NTech.Services.Infrastructure;

namespace nSavings.Code.Eventing
{
    public abstract class EventSubscriberBase
    {
        private static readonly Lazy<ConcurrentStack<string>> SubIds =
            new Lazy<ConcurrentStack<string>>(() => new ConcurrentStack<string>());

        public void OnShutdown(Action<string> unsubscribe)
        {
            while (SubIds.Value.TryPop(out var subId))
            {
                unsubscribe(subId);
            }
        }

        protected static void Subscribe(SavingsEventCode evt, Action<string, CancellationToken> onEvt,
            Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            var subId = subscribe(
                evt.ToString(),
                onEvt);
            SubIds.Value.Push(subId);
        }

        protected NHttp.NHttpCall BeginCallSelf()
        {
            var token = AcquireBearerToken();
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nSavings"]), token);
        }

        private static string AcquireBearerToken()
        {
            return NTechCache.WithCache("nSavingsAutomation.29f2a55a-9a86-44af-a230-b504b3749164",
                TimeSpan.FromMinutes(3), () =>
                {
                    var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
                    return NHttp.AquireSystemUserAccessTokenWithUsernamePassword(credentials.Item1, credentials.Item2,
                        new Uri(NEnv.ServiceRegistry.Internal["nUser"]));
                });
        }
    }
}