using NTech.Services.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace nCredit.Code
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

        protected void Subscribe(CreditEventCode evt, Action<string, CancellationToken> onEvt, Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            var subId = subscribe(
                evt.ToString(),
                (data, ct) => onEvt(data, ct));
            subIds.Value.Push(subId);
        }

        protected NHttp.NHttpCall BeginCallSelf()
        {
            var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
            var token = NHttp.AquireSystemUserAccessTokenWithUsernamePassword(credentials.Item1, credentials.Item2, new Uri(NEnv.ServiceRegistry.Internal["nUser"]));
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nCredit"]), token);
        }
    }
}