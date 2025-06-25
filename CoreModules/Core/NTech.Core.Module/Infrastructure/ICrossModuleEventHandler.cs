using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NTech.Core.Host.Infrastructure
{
    public interface ICrossModuleEventHandler
    {
        string EventName { get; }
        Task HandleEvent(CrossModuleEvent evt, IServiceScope serviceScope, ILogger logger, CancellationToken cancellationToken);
    }
}


