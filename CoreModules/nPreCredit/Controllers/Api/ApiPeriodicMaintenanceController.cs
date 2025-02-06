using nPreCredit.Code;
using nPreCredit.Code.AffiliateReporting;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.DbModel;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    [RoutePrefix("api")]
    public class ApiPeriodicMaintenanceController : NController
    {
        private readonly IApplicationCancellationService applicationCancellationService;

        public ApiPeriodicMaintenanceController(IApplicationCancellationService applicationCancellationService)
        {
            this.applicationCancellationService = applicationCancellationService;
        }

        [Route("PeriodicMaintenance/Run")]
        [HttpPost()]
        public ActionResult RunPeriodicMaintenance(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return PreCreditContext.RunWithExclusiveLock("ntech.scheduledjobs.precreditperiodicmaintenance",
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
                this.Service.Resolve<IEncryptedTemporaryStorageService>().DeleteExpiredItems();
                ApiAutoCancelOldPendingApplications(null);
                OneTimeMigrateCreditDecisionRejectionReasonSearchTerms();
                RunClientOnetimeSqlScripts();
                ReplicateApplicationsToCustomerModule();
                CustomerCheckPointService.MigrateToCustomerModule(
                    Service.Resolve<PreCreditContextFactoryService>(),
                    Service.Resolve<EncryptionService>(),
                    LegacyServiceClientFactory.CreateCustomerClient(
                        LegacyHttpServiceSystemUser.SharedInstance,
                        NEnv.ServiceRegistry),
                    NEnv.ServiceRegistry);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "PreCredit PeriodicMaintenance crashed");
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

        private void RunClientOnetimeSqlScripts()
        {
            var r = new ClientSqlOnetimeScriptRunner(
                Service.Resolve<IKeyValueStoreService>(),
                () => new PreCreditContextExtended(NTechUser, Clock));

            r.RunScriptsForClient();
        }

        private void ApiAutoCancelOldPendingApplications(int? cancelAfterDays)
        {
            if (NEnv.IsStandardUnsecuredLoansEnabled)
            {
                applicationCancellationService.AutoCancelOldPendingApplications(cancelAfterDays);
            }
            else
            {
                LegacyAutoCancelledApplications(cancelAfterDays);
            }
        }

        private void LegacyAutoCancelledApplications(int? cancelAfterDays)
        {
            OneTimeMigrateInCancellations();

            var today = Clock.Now.Date;
            var cancelAfterDate = today.AddDays(-(cancelAfterDays ?? 30));
            var contextFactory = Service.Resolve<IPreCreditContextFactoryService>();
            var repo = Service.Resolve<CreditManagementWorkListService>();
            string[] applicationNrsToCancel;
            using (var context = contextFactory.CreateExtended())
            {
                applicationNrsToCancel = repo
                    .GetSearchModel(context, true)
                    .Where(x =>
                        x.LatestSystemCommentDate.HasValue && x.LatestSystemCommentDate.Value < cancelAfterDate
                        && (
                            x.CategoryCodes.Contains(CreditManagementWorkListService.CreditApplicationCategoryCode.WaitingForData.ToString())
                            || x.CategoryCodes.Contains(CreditManagementWorkListService.CreditApplicationCategoryCode.WaitingForSignature.ToString())
                            || x.CategoryCodes.Contains(CreditManagementWorkListService.CreditApplicationCategoryCode.WaitingForDocument.ToString())
                            || x.CategoryCodes.Contains(CreditManagementWorkListService.CreditApplicationCategoryCode.WaitingForAdditionalInformation.ToString())
                       )
                       && x.IsActive)
                    .Select(x => x.ApplicationNr)
                    .ToArray();
            }

            foreach (var applicationNrGroup in applicationNrsToCancel.SplitIntoGroupsOfN(100))
            {
                var reportingEvents = new List<CreditApplicationCancelledEventModel>();

                using (var context = contextFactory.CreateExtended())
                {
                    var searchModels = repo
                        .GetSearchModel(context, true)
                        .Where(x => applicationNrGroup.Contains(x.ApplicationNr))
                        .Select(x => new { x.ApplicationNr, x.CategoryCodes, x.LatestSystemCommentDate })
                        .ToDictionary(x => x.ApplicationNr);

                    var headers = context
                        .CreditApplicationHeadersQueryable
                        .Where(x => applicationNrGroup.Contains(x.ApplicationNr))
                        .Select(x => new
                        {
                            H = x,
                            ProviderApplicationId = x.Items.Where(y => y.GroupName == "application" && y.Name == "providerApplicationId" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault()
                        })
                        .ToList();

                    var now = Clock.Now;
                    foreach (var header in headers)
                    {
                        var h = header.H;
                        var m = searchModels[h.ApplicationNr];
                        string primaryCategoryCode = CreditManagementWorkListService.PickCancelleationCategoryCode(m.CategoryCodes);

                        var ageInDays = (int)Math.Round(today.Subtract(m.LatestSystemCommentDate.Value.Date).TotalDays);
                        var commentText = $"Application automatically cancelled after {ageInDays} days from category {primaryCategoryCode}";
                        var cancelledState = "Category_" + primaryCategoryCode;

                        //TODO: Remove this cast and move ApplicationCancellationService to shared
                        ApplicationCancellationService.SetApplicationAsCancelled(h, now, true, cancelledState, commentText, CurrentUserId, InformationMetadata, (PreCreditContextExtended)context, header.ProviderApplicationId);

                        if (!string.IsNullOrWhiteSpace(header.ProviderApplicationId))
                        {
                            reportingEvents.Add(new CreditApplicationCancelledEventModel
                            {
                                ApplicationNr = h.ApplicationNr,
                                ProviderApplicationId = header.ProviderApplicationId,
                                WasAutomated = true,
                                CancelledDate = now.DateTime,
                                ProviderName = h.ProviderName
                            });
                        }
                    }

                    context.SaveChanges();
                }
            }
        }

        private void SaveMigrationDoneItem(string name, DateTimeOffset now)
        {
            using (var context = new PreCreditContext())
            {
                context.SystemItems.Add(new SystemItem
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    InformationMetaData = InformationMetadata,
                    Key = name,
                    Value = "true"
                });
                context.SaveChanges();
            }
        }

        private void OneTimeMigrateCreditDecisionRejectionReasonSearchTerms()
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return;
            if (NEnv.IsStandardUnsecuredLoansEnabled) //Standard does not use NEnv.ScoringSetup at all
                return;
            var scoringModel = NEnv.ScoringSetup;
            var now = Clock.Now;
            const string MigrationName = "IsDone_DecisionSearchTermsRejectionsMigration";
            int[] decisionIds;

            using (var context = new PreCreditContext())
            {
                if (context.SystemItems.Any(x => x.Key == MigrationName))
                    return;

                decisionIds = context
                    .CreditDecisions
                    .OfType<RejectedCreditDecision>()
                    .Where(x => !x.SearchTerms.Any())
                    .Select(x => x.Id)
                    .ToArray();
            }

            foreach (var g in decisionIds.SplitIntoGroupsOfN(200))
            {
                using (var context = new PreCreditContext())
                {
                    var decisions = context
                        .CreditDecisions
                        .OfType<RejectedCreditDecision>()
                        .Where(x => g.Contains(x.Id))
                        .Select(x => new
                        {
                            x.RejectedDecisionModel,
                            x.Id
                        })
                        .ToList();
                    foreach (var d in decisions)
                    {
                        var rejectionReasons = CreditDecisionModelParser.ParseRejectionReasons(d.RejectedDecisionModel);
                        if (rejectionReasons != null)
                        {
                            bool hasOther = false;
                            foreach (var r in rejectionReasons.Distinct().ToArray())
                            {
                                if (scoringModel.IsKnownRejectionReason(r) || r.IsOneOf("dbr", "leftToLiveOn")) //dbr and ltl merged in a bad way. will not be done this way going forward
                                {
                                    context.CreditDecisionSearchTerms.Add(new CreditDecisionSearchTerm
                                    {
                                        CreditDecisionId = d.Id,
                                        TermName = CreditDecisionSearchTerm.CreditDecisionSearchTermCode.RejectionReason.ToString(),
                                        TermValue = r
                                    });
                                }
                                else
                                    hasOther = true;
                            }
                            if (hasOther)
                            {
                                context.CreditDecisionSearchTerms.Add(new CreditDecisionSearchTerm
                                {
                                    CreditDecisionId = d.Id,
                                    TermName = CreditDecisionSearchTerm.CreditDecisionSearchTermCode.RejectionReason.ToString(),
                                    TermValue = "other"
                                });
                            }
                        }
                    }
                    context.SaveChanges();
                }
            }
            SaveMigrationDoneItem(MigrationName, now);
        }

        private void OneTimeMigrateInCancellations()
        {
            const string MigrationName = "IsDone_CancelledApplicationsAddedMigration";
            var now = Clock.Now;

            string[] applicationNrs;
            using (var context = new PreCreditContext())
            {
                if (context.SystemItems.Any(x => x.Key == MigrationName))
                    return;

                applicationNrs = context
                    .CreditApplicationHeaders
                    .Where(x => x.IsCancelled && !x.Cancellations.Any())
                    .Select(x => x.ApplicationNr)
                    .ToArray();
            }

            foreach (var g in applicationNrs.SplitIntoGroupsOfN(300))
            {
                using (var context = new PreCreditContext())
                {
                    var apps = context
                        .CreditApplicationHeaders
                        .Where(x => g.Contains(x.ApplicationNr) && x.IsCancelled && !x.Cancellations.Any())
                        .Select(x => new
                        {
                            x.ApplicationNr,
                            x.CancelledBy,
                            x.CancelledDate,
                            x.CancelledState,
                            x.ChangedDate,
                            LastCancelComment = x
                                .Comments
                                .Where(y => y.EventType.StartsWith("ApplicationCancelledWhen"))
                                .OrderByDescending(y => y.Id)
                                .Select(y => y.CommentText)
                                .FirstOrDefault()
                        })
                        .ToArray();
                    foreach (var app in apps)
                    {
                        var wasAutomated = (app.LastCancelComment ?? "").StartsWith("Application automatically cancelled") || (app.LastCancelComment ?? "").StartsWith("Application cancelled by onetime batch close");
                        context.CreditApplicationCancellations.Add(new CreditApplicationCancellation
                        {
                            ApplicationNr = app.ApplicationNr,
                            CancelledBy = app.CancelledBy ?? 0,
                            CancelledDate = app.CancelledDate ?? app.ChangedDate,
                            ChangedById = CurrentUserId,
                            InformationMetaData = InformationMetadata,
                            WasAutomated = wasAutomated,
                            CancelledState = app.CancelledState,
                            ChangedDate = now
                        });
                    }
                    context.SaveChanges();
                }
            }

            SaveMigrationDoneItem(MigrationName, now);
        }

        private void ReplicateApplicationsToCustomerModule()
        {
            var s = new LoanStandardCustomerRelationService(NTechUser, Clock);
            s.SynchronizeExistingApplications();
        }
    }
}