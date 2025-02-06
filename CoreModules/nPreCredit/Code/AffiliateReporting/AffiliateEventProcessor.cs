using nPreCredit.Code.Clients;
using nPreCredit.DbModel;
using NTech.Core;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateEventProcessor : IAffiliateEventProcessor
    {
        private readonly IAffiliateDataSource affiliateDataSource;
        private readonly IAffiliateReportingLogger affiliateReportingLogger;
        private readonly ICoreClock clock;
        private readonly Lazy<NTechSelfRefreshingBearerToken> systemUserBearerToken;
        private readonly Lazy<nPreCreditClient> preCreditClient;

        public AffiliateEventProcessor(IAffiliateDataSource affiliateDataSource, IAffiliateReportingLogger affiliateReportingLogger, ICoreClock clock)
        {
            this.affiliateDataSource = affiliateDataSource;
            this.affiliateReportingLogger = affiliateReportingLogger;
            this.clock = clock;
            var systemUserBearerToken = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            {
                var unp = NEnv.ApplicationAutomationUsernameAndPassword;
                return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, unp.Item1, unp.Item2);
            });
            this.systemUserBearerToken = systemUserBearerToken;
            this.preCreditClient = new Lazy<nPreCreditClient>(() => new nPreCreditClient(systemUserBearerToken.Value.GetToken));
        }

        private IQueryable<AffiliateReportingEvent> PendingEventsQuery(PreCreditContext context, DateTime now)
        {
            return context.AffiliateReportingEvents.Where(x => x.ProcessedStatus == AffiliateReportingEventResultCode.Pending.ToString() && x.WaitUntilDate < now);
        }

        public void ProcessIncomingEvents(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            List<string> providersWithEvents;
            using (var context = new PreCreditContext())
            {
                providersWithEvents = PendingEventsQuery(context, now)
                        .Select(x => x.ProviderName)
                        .Distinct()
                        .ToList();
            }

            foreach (var providerName in providersWithEvents)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                try
                {
                    HandleProvider(providerName, now, cancellationToken);
                }
                catch (Exception ex)
                {
                    NLog.Error(ex, $"HandleProvider failed for {providerName}");
                }
            }

            DeleteOld();
        }

        private void HandleProvider(string providerName, DateTime now, CancellationToken cancellationToken)
        {
            List<long> eventIds;

            using (var context = new PreCreditContext())
            {
                eventIds = PendingEventsQuery(context, now)
                    .Where(x => x.ProviderName == providerName)
                    .Select(x => x.Id)
                    .ToList();
            }

            foreach (var eventId in eventIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                AffiliateReportingEvent evt;
                using (var context = new PreCreditContext())
                {
                    evt = context.AffiliateReportingEvents.Single(x => x.Id == eventId);
                }

                HandleEventResult r;
                try
                {
                    r = HandleEvent(evt);
                }
                catch (Exception ex)
                {
                    r = new HandleEventResult
                    {
                        Message = "Exception in HandleEvent. Contact support.",
                        Exception = ex,
                        Status = AffiliateReportingEventResultCode.Error
                    };
                }

                affiliateReportingLogger.Log(evt.Id, evt.ProviderName, r);

                AddCommentOnApplication(evt, r);
            }
        }

        private void AddCommentOnApplication(AffiliateReportingEvent evt, HandleEventResult r)
        {
            if (r.Status == AffiliateReportingEventResultCode.Ignored || r.Status == AffiliateReportingEventResultCode.Pending)
                return;

            try
            {
                preCreditClient.Value.AddCommentToApplication(
                    evt.ApplicationNr,
                    r.Status == AffiliateReportingEventResultCode.Success
                        ? $"{evt.EventType} reported to provider. See provider log for details."
                        : $"Failed to report {evt.EventType} to provider. See provider log for details.",
                    null,
                    null,
                    "AffiliateReporting");
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Failed to write a comment on {evt.ApplicationNr} for affiliate event {evt.Id}");
            }
        }

        private HandleEventResult HandleEvent(AffiliateReportingEvent evt)
        {
            var throttlingPolicy = affiliateDataSource.GetThrottlingPolicy(evt.ProviderName);
            if (throttlingPolicy?.IsThrottled(evt.ProviderName, AffiliateCallbackThrottlingPolicy.StandardContextName) ?? false)
            {
                return new HandleEventResult
                {
                    Status = AffiliateReportingEventResultCode.Pending,
                    Message = "Throttled due to affiliate api call count limitations"
                };
            }

            var dispatcher = affiliateDataSource.GetDispatcher(evt.ProviderName);

            if (dispatcher == null)
                return new HandleEventResult
                {
                    Status = AffiliateReportingEventResultCode.Ignored,
                    Message = "Affiliate has no callbacks"
                };

            return dispatcher.Dispatch(evt);
        }

        private void DeleteOld()
        {
            var now = clock.Now;
            using (var context = new PreCreditContext())
            {
                foreach (var evt in context.AffiliateReportingEvents.Where(x => x.DeleteAfterDate < now).ToList())
                {
                    context.AffiliateReportingEvents.Remove(evt);
                }
                foreach (var item in context.AffiliateReportingLogItems.Where(x => x.DeleteAfterDate < now).ToList())
                {
                    context.AffiliateReportingLogItems.Remove(item);
                }

                context.SaveChanges();
            }
        }
    }
}