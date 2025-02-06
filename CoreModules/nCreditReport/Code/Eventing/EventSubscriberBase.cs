using System;
using System.Collections.Concurrent;
using System.Threading;

namespace nCreditReport.Code
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

        protected void Subscribe(CreditReportEventCode evt, Action<string, CancellationToken> onEvt, Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            var subId = subscribe(
                evt.ToString(),
                (data, ct) => onEvt(data, ct));
            subIds.Value.Push(subId);
        }
    }
}