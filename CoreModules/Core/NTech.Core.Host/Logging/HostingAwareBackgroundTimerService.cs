namespace NTech.Services.Infrastructure
{
    public abstract class HostingAwareBackgroundTimerService : IHostedService
    {
        private DateTimeOffset? lastErrorLogTime = null;
        private int nrOfErrorsSinceLastHandleCall = 0;

        private bool ShouldLogOnTickError(DateTimeOffset now)
        {
            if (!lastErrorLogTime.HasValue)
                return true;
            return lastErrorLogTime.Value.Add(ErrorLogFrequency) < now;
        }

        protected virtual void LogOnTickError(Exception lastException, int nrOfErrorsSinceLastHandleCall)
        {
        }

        protected virtual TimeSpan ErrorLogFrequency => TimeSpan.FromMinutes(30);
        protected virtual TimeSpan? DelayBeforeFirstTick => TimeSpan.Zero;

        private CancellationTokenSource runningToken;

        protected abstract Task OnTick(CancellationToken cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (runningToken != null)
                return Task.CompletedTask;

            runningToken = new CancellationTokenSource();

            var t = runningToken.Token;
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    if (DelayBeforeFirstTick.HasValue)
                        WaitHandle.WaitAny(new[] { t.WaitHandle }, DelayBeforeFirstTick.Value);

                    while (!t.IsCancellationRequested)
                    {
                        WaitHandle.WaitAny(new[] { t.WaitHandle }, TimeBetweenTicksInMilliseconds);

                        try
                        {
                            await OnTick(t);
                        }
                        catch (Exception ex)
                        {
                            var now = DateTimeOffset.Now;
                            if (ShouldLogOnTickError(now))
                            {
                                LogOnTickError(ex, nrOfErrorsSinceLastHandleCall + 1);
                                nrOfErrorsSinceLastHandleCall = 0;
                                lastErrorLogTime = now;
                            }
                            else
                            {
                                nrOfErrorsSinceLastHandleCall += 1;
                            }
                        }

                        if (cancellationToken.IsCancellationRequested)
                            break;
                    }
                }
                finally
                {
                    runningToken = null;
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var t = runningToken;

            t?.Cancel();
            return Task.CompletedTask;
        }

        protected abstract int TimeBetweenTicksInMilliseconds { get; }
        protected abstract string Name { get; }
    }

    public class BackgroundServiceStarter<T> : IHostedService where T : IHostedService
    {
        private readonly T backgroundService;

        public BackgroundServiceStarter(T backgroundService)
        {
            this.backgroundService = backgroundService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return backgroundService.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return backgroundService.StopAsync(cancellationToken);
        }
    }
}