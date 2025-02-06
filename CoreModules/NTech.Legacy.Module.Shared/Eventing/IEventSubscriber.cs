using System;
using System.Threading;

namespace NTech.Services.Infrastructure.Eventing
{
    public interface IEventSubscriber
    {
        void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe);
        void OnShutdown(Action<string> unsubscribe);
    }
}
