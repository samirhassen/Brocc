using Autofac;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Threading;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateReportingBackgroundTimer : HostingAwareBackgroundTimer
    {
        public AffiliateReportingBackgroundTimer()
        {

        }

        protected override int TimeBetweenTicksInMilliseconds => 5000;

        protected override string Name => nameof(AffiliateReportingBackgroundTimer);

        protected override void OnTick(CancellationToken cancellationToken)
        {
            DependancyInjection.WithAffiliateReportingBackgroundSeviceScope(scope =>
            {
                var p = scope.Resolve<IAffiliateEventProcessor>();
                p.ProcessIncomingEvents(cancellationToken);
            });
        }

        protected override void LogOnTickError(Exception lastException, int nrOfErrorsSinceLastHandleCall)
        {
            NLog.Error(lastException, $"Error in AffiliateReportingBackgroundTimer. There have been {nrOfErrorsSinceLastHandleCall} errors since the last logged including this one.");
        }
    }
}