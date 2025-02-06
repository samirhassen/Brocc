using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace NTech.Core.Host.Infrastructure
{
    public class CrossModuleEventQueue : ICrossModuleEventQueue
    {
        private readonly Channel<CrossModuleEvent> queue;
        private readonly ILogger<CrossModuleEventQueue> logger;
        private readonly IServiceProvider service;
        private readonly ConcurrentDictionary<string, ICrossModuleEventHandler> eventHandlers;

        public CrossModuleEventQueue(ILogger<CrossModuleEventQueue> logger, IServiceProvider service)
        {
            var opts = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
            queue = Channel.CreateBounded<CrossModuleEvent>(opts);
            this.logger = logger;
            this.service = service;
            this.eventHandlers = new ConcurrentDictionary<string, ICrossModuleEventHandler>();
        }

        public void AddEventHandlerIfNotPresent(string id, Func<ICrossModuleEventHandler> createHandler)
        {
            eventHandlers.AddOrUpdate(id, (_) => createHandler(), (_, oldValue) => oldValue);
        }

        public bool RemoveEventHandler(string id)
        {
            return eventHandlers.TryRemove(id, out var _);
        }

        public async Task Process(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var evt = await DequeueEventAsync(stoppingToken);

                logger.LogInformation($"Cross module event: Processing {evt.EventName}");

                using var scope = service.CreateScope();
                foreach (var handler in eventHandlers.Values)
                {
                    if (handler.EventName == evt.EventName)
                    {
                        await handler.HandleEvent(evt, scope, logger, stoppingToken);
                    }
                }
            }
        }

        public async ValueTask QueueEventAsync([NotNull] CrossModuleEvent evt)
        {
            await queue.Writer.WriteAsync(evt);
            logger.LogInformation($"Cross module event: Queued {evt?.EventName}");
        }

        public async ValueTask<CrossModuleEvent> DequeueEventAsync(CancellationToken cancellationToken)
        {
            var evt = await queue.Reader.ReadAsync(cancellationToken);
            logger.LogInformation($"Cross module event: Dequeued {evt?.EventName}");
            return evt;
        }
    }
}


