using nPreCredit.Code.AffiliateReporting;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationCancellationService : IApplicationCancellationService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;
        private readonly IMinimalSharedWorkflowService workflowService;
        private readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly CreditManagementWorkListService creditManagementWorkListService;

        public ApplicationCancellationService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock, IMinimalSharedWorkflowService workflowService,
            IPreCreditContextFactoryService contextFactoryService, CreditManagementWorkListService creditManagementWorkListService)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
            this.workflowService = workflowService;
            this.contextFactoryService = contextFactoryService;
            this.creditManagementWorkListService = creditManagementWorkListService;
        }

        public bool TryCancelApplication(string applicationNr, out string failedMessage, bool isAutomatic = false)
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var appPre = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        App = x,
                        ListNames = x.ListMemberships.Select(y => y.ListName),
                        IsMortgageLoan = x.MortgageLoanExtension != null,
                        ProviderApplicationId = x.Items.Where(y => y.GroupName == "application" && y.Name == "providerApplicationId" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault()
                    })
                    .Single();
                var app = appPre.App;

                if (app.IsFinalDecisionMade)
                {
                    failedMessage = $"Attempted to cancel application {applicationNr} where the credit has already been created";
                    return false;
                }

                if (!app.IsActive)
                {
                    failedMessage = $"Attempted to cancel application {applicationNr} that is not active";
                    return false;
                }

                if (app.IsCancelled)
                {
                    failedMessage = $"Attempted to cancel application {applicationNr} that is already cancelled";
                    return false;
                }

                var now = context.Clock.Now;
                CancelApplicationComposable(context, app, appPre.ListNames, appPre.ProviderApplicationId, () =>
                {
                    var m = creditManagementWorkListService.GetSearchModel(context, true).Where(x => x.ApplicationNr == applicationNr).SingleOrDefault();
                    var primaryCategoryCode = CreditManagementWorkListService.PickCancelleationCategoryCode(m.CategoryCodes);
                    return Tuple.Create(
                        "Category_" + primaryCategoryCode,
                        $"Application manually cancelled from category {primaryCategoryCode}");
                },
                isAutomatic);

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        private void CancelApplicationComposable(PreCreditContextExtended context, CreditApplicationHeader app, IEnumerable<string> listNames, string providerApplicationId, Func<Tuple<string, string>> onLegacyApplication, bool isAutomatic = false)
        {
            string cancelledState;
            string commentText;
            var now = context.Clock.Now;
            if (app.ApplicationType == CreditApplicationTypeCode.companyLoan.ToString())
            {
                var currentInitialListName = workflowService.GetEarliestInitialListName(listNames);
                cancelledState = workflowService.GetStepDisplayName(currentInitialListName) ?? "Unknown";
                commentText = $"Application manually cancelled from '{cancelledState}'";
            }
            else if (app.ApplicationType == CreditApplicationTypeCode.mortgageLoan.ToString())
            {
                var currentInitialListName = workflowService.GetEarliestInitialListName(listNames);
                cancelledState = workflowService.GetStepDisplayName(currentInitialListName) ?? "Unknown";
                commentText = $"Application manually cancelled from '{cancelledState}'";
            }
            else if (app.ApplicationType == CreditApplicationTypeCode.unsecuredLoan.ToString() && NEnv.IsStandardUnsecuredLoansEnabled)
            {
                var cancelledType = isAutomatic ? "automatically" : "manually";
                var currentInitialListName = workflowService.GetEarliestInitialListName(listNames);
                cancelledState = workflowService.GetStepDisplayName(currentInitialListName) ?? "Unknown";
                commentText = $"Application {cancelledType} cancelled from '{cancelledState}'";
            }
            else
            {
                var result = onLegacyApplication();
                cancelledState = result.Item1;
                commentText = result.Item2;
            }

            SetApplicationAsCancelled(app, now, false, cancelledState, commentText, context.CurrentUserId, context.InformationMetadata, context, providerApplicationId);
        }

        public bool TryReactivateApplication(string applicationNr, out string failedMessage)
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);

                if (app.IsFinalDecisionMade)
                {
                    NLog.Warning("Reactivate got called on an application that is already a loan. This is not expected. {applicationNr}", applicationNr);
                    failedMessage = $"Attempted to reactivate application {applicationNr} where the credit has already been created";
                    return false;
                }

                if (!(app.IsCancelled || app.IsRejected))
                {
                    failedMessage = $"Attempted to reactivate application {applicationNr} that is not cancelled or rejected";
                    return false;
                }

                if (app.IsActive)
                {
                    failedMessage = $"Attempted to reactivate application {applicationNr} that is already active";
                    return false;
                }

                context.CreateAndAddComment("Application reactivated", "ApplicationReactivated", applicationNr: applicationNr);

                app.IsActive = true;

                if (app.IsCancelled)
                {
                    app.IsCancelled = false;
                    app.CancelledBy = null;
                    app.CancelledDate = null;
                    app.CancelledState = null;
                }

                if (app.IsRejected)
                {
                    app.IsRejected = false;
                    app.RejectedById = null;
                    app.RejectedDate = null;
                    app.RejectedById = null;
                    app.RejectedDate = null;
                }

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        public void AutoCancelOldPendingApplications(int? cancelAfterDays)
        {
            var today = clock.Now.Date;
            var cancelAfterDate = today.AddDays(-(cancelAfterDays ?? 30));
            string[] applicationNrsToCancel;
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                applicationNrsToCancel = context
                    .CreditApplicationHeaders
                    .Where(x =>
                        x.IsActive
                        && !x.IsFinalDecisionMade
                        && x.ApplicationDate < cancelAfterDate
                        && !x.Comments.Any(y => y.EventType != "UserComment" && y.CommentDate >= cancelAfterDate))
                    .Select(x => x.ApplicationNr)
                    .ToArray();
            }

            foreach (var applicationNrGroup in applicationNrsToCancel.SplitIntoGroupsOfN(100))
            {
                using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
                {
                    var appsPre = context
                        .CreditApplicationHeaders
                        .Where(x => applicationNrGroup.Contains(x.ApplicationNr))
                        .Select(x => new
                        {
                            App = x,
                            ListNames = x.ListMemberships.Select(y => y.ListName),
                            ProviderApplicationId = x.Items.Where(y => y.GroupName == "application" && y.Name == "providerApplicationId" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault()
                        })
                        .ToList();
                    var now = context.Clock.Now;
                    foreach (var appPre in appsPre)
                    {
                        CancelApplicationComposable(context, appPre.App, appPre.ListNames, appPre.ProviderApplicationId, () =>
                        {
                            throw new Exception("Legacy applications are handled by ApiPeriodicMaintenanceController.LegacyAutoCancelledApplications");
                        });
                    }
                    context.SaveChanges();
                }
            }
        }

        public static void SetApplicationAsCancelled(CreditApplicationHeader h, DateTimeOffset now, bool wasAutomated, string cancelledState, string commentText, int currentUserId, string informationMetadata, IPreCreditContextExtended context, string providerApplicationId)
        {
            h.IsActive = false;
            h.IsCancelled = true;
            h.CancelledBy = currentUserId;
            h.CancelledDate = now;
            h.CancelledState = cancelledState;
            context.AddCreditApplicationComments(new CreditApplicationComment
            {
                ApplicationNr = h.ApplicationNr,
                CommentText = commentText,
                CommentById = currentUserId,
                ChangedById = currentUserId,
                CommentDate = now,
                ChangedDate = now,
                EventType = "ApplicationCancelledWhen" + h.CancelledState,
                InformationMetaData = informationMetadata
            });
            context.AddCreditApplicationCancellations(new CreditApplicationCancellation
            {
                ApplicationNr = h.ApplicationNr,
                CancelledBy = currentUserId,
                CancelledDate = now,
                CancelledState = cancelledState,
                InformationMetaData = informationMetadata,
                WasAutomated = wasAutomated,
                ChangedDate = now,
                ChangedById = currentUserId
            });

            if (!string.IsNullOrWhiteSpace(providerApplicationId))
            {
                AffiliateReportingService.AddCreditApplicationCancelledEventsComposable(
                    new List<CreditApplicationCancelledEventModel>
                    {
                    new CreditApplicationCancelledEventModel
                    {
                        ApplicationNr = h.ApplicationNr,
                        ProviderApplicationId = providerApplicationId,
                        WasAutomated = wasAutomated,
                        CancelledDate = now.DateTime,
                        ProviderName = h.ProviderName
                    }
                    },
                    context,
                    now.DateTime);
            }
        }

    }

    public interface IApplicationCancellationService
    {
        bool TryCancelApplication(string applicationNr, out string failedMessage, bool isAutomatic = false);

        bool TryReactivateApplication(string applicationNr, out string failedMessage);

        void AutoCancelOldPendingApplications(int? cancelAfterDays);
    }
}