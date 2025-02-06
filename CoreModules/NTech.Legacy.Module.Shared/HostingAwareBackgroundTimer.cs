using Serilog;
using System;
using System.Threading;
using System.Web.Hosting;

namespace NTech.Services.Infrastructure
{
    public abstract class HostingAwareBackgroundTimer
    {
        private DateTimeOffset? lastErrorLogTime = null;
        private int nrOfErrorsSinceLastHandleCall = 0;

        public void Start(TimeSpan? delayBeforeFirstTick)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            HostingEnvironment.QueueBackgroundWorkItem(ct =>
            {
                try
                {
                    if (delayBeforeFirstTick.HasValue)
                        WaitHandle.WaitAny(new[] { ct.WaitHandle }, delayBeforeFirstTick.Value);
                    NLog.Debug($"Starting HostingAwareBackgroundTimer {Name}");
                    while (true)
                    {
                        WaitHandle.WaitAny(new[] { ct.WaitHandle }, TimeBetweenTicksInMilliseconds);

                        NLog.Debug($"Ticking HostingAwareBackgroundTimer {Name}");
                        try
                        {
                            OnTick(ct);
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

                        if (ct.IsCancellationRequested)
                            break;
                    }
                }
                finally
                {
                    IsRunning = false;
                    NLog.Debug($"Exiting HostingAwareBackgroundTimer {Name}");
                }
            });
        }

        private bool ShouldLogOnTickError(DateTimeOffset now)
        {
            if (!lastErrorLogTime.HasValue)
                return true;
            return lastErrorLogTime.Value.Add(ErrorLogFrequency) < now;
        }

        protected abstract void LogOnTickError(Exception lastException, int nrOfErrorsSinceLastHandleCall);

        protected virtual TimeSpan ErrorLogFrequency { get { return TimeSpan.FromMinutes(30); } }

        public bool IsRunning { get; set; }

        protected abstract void OnTick(CancellationToken cancellationToken);
        protected abstract int TimeBetweenTicksInMilliseconds { get; }
        protected abstract string Name { get; }
    }
}
