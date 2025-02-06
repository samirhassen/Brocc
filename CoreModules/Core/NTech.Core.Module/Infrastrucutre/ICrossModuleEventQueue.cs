using System.Diagnostics.CodeAnalysis;

namespace NTech.Core.Host.Infrastructure
{
    public interface ICrossModuleEventQueue
    {
        ValueTask QueueEventAsync([NotNull] CrossModuleEvent evt);
        void AddEventHandlerIfNotPresent(string id, Func<ICrossModuleEventHandler> createHandler);
        bool RemoveEventHandler(string id);
    }
}


