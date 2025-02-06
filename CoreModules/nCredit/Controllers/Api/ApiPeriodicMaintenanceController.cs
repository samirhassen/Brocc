using nCredit.Code;
using nCredit.Code.Email;
using nCredit.DbModel.BusinessEvents;
using NTech.Core;
using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/PeriodicMaintenance")]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiPeriodicMaintenanceController : NController
    {
        [Route("Run")]
        [HttpPost()]
        public ActionResult RunPeriodicMaintenance(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.creditperiodicmaintenance",
                    () => RunPeriodicMaintenanceI(),
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private ActionResult RunPeriodicMaintenanceI()
        {
            List<string> errors = new List<string>();
            var w = Stopwatch.StartNew();
            try
            {
                //NOTE: Anything that is added to run here must support running with any frequency without causing problems
                PopulateCalendarDates(CoreClock.SharedInstance, Service.CalendarDateService);
                RunExactlyOnce("PopulateApplicationNrWhenMissing", PopulateApplicationNrWhenMissing);
                RunExactlyOnce("FixRemovePromisedToPayBug", FixRemovePromisedToPayBug);
                RunExactlyOnce("FixTermsChangesNotCancelledOnDebtCollectionExport", FixTermsChangesNotCancelledOnDebtCollectionExport);
                RunExactlyOnce("FixWrongEventTypeForReminderComments", FixWrongEventTypeForReminderComments);
                RunExactlyOnce("AddCampaignCodesToLegacyCredits", AddCampaignCodesToLegacyCredits);
                if (!NEnv.IsStandardMortgageLoansEnabled)
                {
                    AutoCancelOldPendingCreditTermChanges();
                }
                AutoCancelOldPendingExpiredCreditSettlementSuggestions();
                this.Service.CustomerRelationsMerge.MergeLoansToCustomerRelations();
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Credit PeriodicMaintenance crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
            finally
            {
                w.Stop();
            }

            NLog.Information("Credit PeriodicMaintenance finished TotalMilliseconds={totalMilliseconds}", w.ElapsedMilliseconds);

            //Used by nScheduler
            var warnings = new List<string>();
            errors?.ForEach(x => warnings.Add(x));

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
        }

        private void AutoCancelOldPendingExpiredCreditSettlementSuggestions()
        {
            new CreditSettlementSuggestionBusinessEventManager(GetCurrentUserMetadata(), CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings,
                    Service.PaymentAccount, Service.ContextFactory, SerilogLoggingService.SharedInstance, Service.RseService,
                    Service.PaymentOrder, EmailServiceFactory.SharedInstance, 
                    LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry))
                .AutoCancelOldPendingExpired();
        }

        private void AutoCancelOldPendingCreditTermChanges()
        {

            new CreditTermsChangeBusinessEventManager(GetCurrentUserMetadata(), Service.LegalInterestCeiling,
                CoreClock.SharedInstance, NEnv.ClientCfgCore, Service.ContextFactory, NEnv.EnvSettings, EmailServiceFactory.SharedInstance,
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry), 
                new SerilogLoggingService(), Service.ServiceRegistry, x => NEnv.GetAffiliateModel(x)).AutoCancelOldPendingTermChanges();
        }

        /// <summary>
        /// Earlier versions didn't have application nr sent to the credit module on creation.
        /// This tries to fix that where possible
        /// </summary>
        private void PopulateApplicationNrWhenMissing(string name)
        {
            var service = Service;
            var preCreditClient = LegacyServiceClientFactory.CreatePreCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var mgr = new PopulateApplicationNrWhenMissingBusinessEventManager(GetCurrentUserMetadata(), CoreClock.SharedInstance, NEnv.ClientCfgCore,
                service.ServiceRegistry, service.ContextFactory, preCreditClient);
            var count = mgr.PopulateReturningCount();
            Log.Information($"PeriodicMaintenance: {name} populated applicationnr on {count} credits");
        }

        /// <summary>
        /// Reminder comments where given the event type BusinessEvent_NewNotification instead of BusinessEvent_NewReminder.
        /// </summary>
        private void FixWrongEventTypeForReminderComments(string name)
        {
            using (var context = new CreditContext())
            {
                context.Database.ExecuteSqlCommand("update CreditComment set EventType = 'BusinessEvent_NewReminder' where EventType = 'BusinessEvent_NewNotification' and CommentText like 'Reminder%'");
            }
        }

        private void AddCampaignCodesToLegacyCredits(string name)
        {
            List<string> providerNames;
            using (var context = new CreditContext())
            {
                providerNames = context.CreditHeaders.Select(x => x.ProviderName).Distinct().ToList();
            }

            foreach (var providerName in providerNames)
            {
                var affiliate = NEnv.GetAffiliateModel(providerName, allowMissing: true);
                if (!string.IsNullOrWhiteSpace(affiliate?.FallbackCampaignCode))
                {
                    using (var context = new CreditContext())
                    {
                        var creditNrs = context
                            .CreditHeaders
                            .Where(x => x.ProviderName == providerName && !x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.IntialLoanCampaignCode.ToString()))
                            .Select(x => x.CreditNr)
                            .ToArray();

                        foreach (var creditNrGroup in creditNrs.SplitIntoGroupsOfN(100))
                        {
                            var credits = context
                                .CreditHeaders
                                .Where(x => creditNrGroup.Contains(x.CreditNr))
                                .Select(x => new
                                {
                                    H = x,
                                    E = x.CreatedByEvent
                                })
                                .ToList();
                            foreach (var c in credits)
                            {
                                context.DatedCreditStrings.Add(new DatedCreditString
                                {
                                    BusinessEvent = c.E,
                                    ChangedDate = Clock.Now,
                                    ChangedById = CurrentUserId,
                                    Credit = c.H,
                                    Name = DatedCreditStringCode.IntialLoanCampaignCode.ToString(),
                                    InformationMetaData = InformationMetadata,
                                    TransactionDate = c.E.TransactionDate,
                                    Value = affiliate?.FallbackCampaignCode
                                });
                            }
                            context.SaveChanges();
                        }
                    }
                }
            }
        }

        public static void PopulateCalendarDates(ICoreClock clock, CalendarDateService calendarDateService)
        {
            var toDate = DateTime.Today.AddYears(20);
            if (!NEnv.IsProduction)
            {
                //If you change this, make sure that the calendar table is always continous even when jumping back and forth through time
                if (clock.Today > toDate)
                    throw new Exception("The calendar table cannot support timemachine dates more than 20 years into the future at this point");
            }
            calendarDateService.EnsureCalendarDates(toDate);
        }

        /// <summary>
        /// Removing promised to pay set RemovedByBusinessEventId on the wrong item initially.
        /// This removes any incorrect removalids
        /// </summary>
        private void FixRemovePromisedToPayBug(string name)
        {
            using (var context = new CreditContext())
            {
                var itemsWithWrongRemovedId = context
                    .DatedCreditDates
                    .Where(x =>
                        x.RemovedByBusinessEventId.HasValue
                        && x.RemovedByBusinessEvent.EventType == BusinessEventType.RemovedPromisedToPayDate.ToString()
                        && x.BusinessEvent.EventType != BusinessEventType.AddedPromisedToPayDate.ToString())
                    .Select(x => new
                    {
                        D = x,
                        BusinessEvent = x.BusinessEvent
                    })
                    .GroupBy(x => x.D.CreditNr)
                    .ToList();

                foreach (var g in itemsWithWrongRemovedId)
                {
                    var creditNr = g.Key;
                    foreach (var d in g)
                    {
                        d.D.RemovedByBusinessEventId = null;
                        d.D.ChangedById = CurrentUserId;
                        d.D.ChangedDate = Clock.Now;
                    }
                    var desc = string.Join(", ", g.Select(x => x.BusinessEvent.EventType).Distinct());
                    context.CreditComments.Add(new CreditComment
                    {
                        CreditNr = creditNr,
                        ChangedById = CurrentUserId,
                        ChangedDate = Clock.Now,
                        CommentById = CurrentUserId,
                        CommentDate = Clock.Now,
                        CommentText = $"Automated correction of PromisedToPayDate bug for events: {desc}",
                        EventType = "BusinessEvent_" + BusinessEventType.Correction.ToString()
                    });
                }
                context.SaveChanges();
            }
        }

        private void FixTermsChangesNotCancelledOnDebtCollectionExport(string name)
        {
            using (var context = CreateCreditContext())
            {
                var ids = context
                    .CreditTermsChangeHeaders
                    .Where(x => !x.CancelledByEventId.HasValue && !x.CommitedByEventId.HasValue && x.Credit.Status == CreditStatus.SentToDebtCollection.ToString())
                    .Select(x => x.Id)
                    .ToList();
                var m = new CreditTermsChangeBusinessEventManager(GetCurrentUserMetadata(), Service.LegalInterestCeiling,
                    CoreClock.SharedInstance, NEnv.ClientCfgCore, Service.ContextFactory, NEnv.EnvSettings, EmailServiceFactory.SharedInstance,
                    LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry), 
                    new SerilogLoggingService(), Service.ServiceRegistry, x => NEnv.GetAffiliateModel(x));
                foreach (var id in ids)
                {
                    string failedMessage;
                    if (!m.TryCancelCreditTermsChange(context, id, false, out failedMessage, additionalReasonMessage: " by one time bugfix for debt collection export not cancelling pending terms changes."))
                    {
                        throw new Exception($"FixTermsChangesNotCancelledOnDebtCollectionExport failed: {failedMessage}");
                    }
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Make sure that the reason these should run only once is for performance reasons not that 
        /// the system dies if it runs more than once.
        /// </summary>
        private void RunExactlyOnce(string name, Action<string> a)
        {
            var key = $"PeriodicMaintenance_HasRunExactlyOnce_{name}";
            using (var context = new CreditContext())
            {
                if (context.SystemItems.Any(x => x.Key == key))
                {
                    Log.Debug($"PeriodicMaintenance: Skipping {name} since it has already run");
                    return;
                }
            }

            Log.Information($"PeriodicMaintenance: Starting exactly once job {name}");
            a(name);

            using (var context = new CreditContext())
            {
                context.SystemItems.Add(new SystemItem
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = DateTime.Now,
                    InformationMetaData = InformationMetadata,
                    Key = key,
                    Value = "true"
                });

                context.SaveChanges();
            }
            Log.Information($"PeriodicMaintenance: Completed exactly once job {name}");
        }
    }
}