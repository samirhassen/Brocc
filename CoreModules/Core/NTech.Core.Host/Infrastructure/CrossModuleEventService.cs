namespace NTech.Core.Host.Infrastructure
{
    public class CrossModuleEventService : BackgroundService
    {
        private readonly CrossModuleEventQueue crossModuleEventQueue;
        private readonly ILogger<CrossModuleEventService> logger;

        public CrossModuleEventService(CrossModuleEventQueue crossModuleEventQueue, ILogger<CrossModuleEventService> logger)
        {
            this.crossModuleEventQueue = crossModuleEventQueue;
            this.logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await crossModuleEventQueue.Process(stoppingToken);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Cross module event: Failure while processing queue");
            }
        }
    }
}


