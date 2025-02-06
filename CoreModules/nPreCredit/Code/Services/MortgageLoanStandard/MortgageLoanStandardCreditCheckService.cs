using Newtonsoft.Json;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.WebserviceMethods;
using nPreCredit.WebserviceMethods.MortgageLoanStandard;
using NTech;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanStandardCreditCheckService : LoanStandardCreditCheckBaseService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IMortgageLoanStandardWorkflowService workflowService;
        private readonly IClock clock;

        public MortgageLoanStandardCreditCheckService(IClock clock, INTechCurrentUserMetadata ntechCurrentUserMetadata, IMortgageLoanStandardWorkflowService workflowService, 
            IApplicationCommentService applicationCommentService, IPreCreditEnvSettings envSettings, IPreCreditContextFactoryService preCreditContextFactory,
            ICustomerClient customerClient, IMarkdownTemplateRenderingService templateRenderingService, INTechEmailServiceFactory emailServiceFactory, LoanStandardEmailTemplateService emailTemplateService,
            INTechServiceRegistry serviceRegistry) : base(applicationCommentService, envSettings, preCreditContextFactory, customerClient, templateRenderingService, emailServiceFactory, emailTemplateService, serviceRegistry)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.workflowService = workflowService;
            this.clock = clock;
        }

        public CreditDecision AcceptInitialCreditDecision(string applicationNr, SetMortgageLoanStandardCurrentCreditDecisionMethod.Request.InitialOfferModel offer, bool wasAutomated, bool supressOfferNotification, MortgageLoanStandardCreditRecommendationModel recommendation)
        {
            return CommitInitialCreditDecision(applicationNr, offer, null, null, wasAutomated, supressOfferNotification, recommendation);
        }

        public CreditDecision AcceptFinalCreditDecision(string applicationNr, SetMortgageLoanStandardCurrentCreditDecisionMethod.Request.FinalOfferModel offer, bool wasAutomated, bool supressOfferNotification, MortgageLoanStandardCreditRecommendationModel recommendation)
        {
            return CommitInitialCreditDecision(applicationNr, null, offer, null, wasAutomated, supressOfferNotification, recommendation);
        }

        public CreditDecision RejectCreditDecision(string applicationNr, SetMortgageLoanStandardCurrentCreditDecisionMethod.Request.RejectionModel rejection, bool wasAutomated, bool supressRejectionNotification, MortgageLoanStandardCreditRecommendationModel recommendation)
        {
            return CommitInitialCreditDecision(applicationNr, null, null, rejection, wasAutomated, supressRejectionNotification, recommendation);
        }

        private CreditDecision CommitInitialCreditDecision(
            string applicationNr,
            SetMortgageLoanStandardCurrentCreditDecisionMethod.Request.InitialOfferModel initialOffer,
            SetMortgageLoanStandardCurrentCreditDecisionMethod.Request.FinalOfferModel finalOffer,
            SetMortgageLoanStandardCurrentCreditDecisionMethod.Request.RejectionModel rejection,
            bool wasAutomated,
            bool supressUserNotification,
            MortgageLoanStandardCreditRecommendationModel recommendation)
        {
            CreditDecision d;
            DecisionNotificationData notificationData;

            var isFinal = (rejection?.IsFinal ?? false) || finalOffer != null;
            var isAccepted = rejection == null;

            void AddDecisionItem(string itemName, string value, bool isRepeatable) =>
                d.DecisionItems.Add(new CreditDecisionItem { IsRepeatable = isRepeatable, ItemName = itemName, Value = value });

            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var tr = context.Database.BeginTransaction();
                try
                {
                    if (initialOffer != null)
                    {
                        d = context.FillInfrastructureFields(new AcceptedCreditDecision { AcceptedDecisionModel = CreditRecommendationUlStandardService.DecisionModelMarker.Value, DecisionItems = new List<CreditDecisionItem>() });

                        var loanAmount = initialOffer.SettlementAmount.GetValueOrDefault()
                            + initialOffer.ObjectPriceAmount.GetValueOrDefault()
                            + initialOffer.PaidToCustomerAmount.GetValueOrDefault()
                            - initialOffer.OwnSavingsAmount.GetValueOrDefault();
                        AddDecisionItem("isPurchase", initialOffer.IsPurchase == true ? "true" : "false", false);
                        AddDecisionItem("objectPriceAmount", initialOffer.ObjectPriceAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("paidToCustomerAmount", initialOffer.PaidToCustomerAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("ownSavingsAmount", initialOffer.OwnSavingsAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("mortgageLoansToSettleAmount", initialOffer.SettlementAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("loanAmount", loanAmount.ToString(CultureInfo.InvariantCulture), false);
                    }
                    else if (rejection != null)
                    {
                        d = context.FillInfrastructureFields(new RejectedCreditDecision { RejectedDecisionModel = CreditRecommendationUlStandardService.DecisionModelMarker.Value, DecisionItems = new List<CreditDecisionItem>() });
                        foreach (var rejectionReason in rejection.RejectionReasons)
                        {
                            AddDecisionItem("rejectionReason", rejectionReason.Code, true);
                            AddDecisionItem("rejectionReasonText", rejectionReason.DisplayName, true);
                        }
                        if (!string.IsNullOrWhiteSpace(rejection.OtherText))
                            AddDecisionItem("rejectionReasonOtherText", rejection.OtherText, false);
                    }
                    else if (finalOffer != null)
                    {
                        throw new NotImplementedException();
                    }
                    else
                        throw new NotImplementedException();


                    if (recommendation != null)
                    {
                        var recommendationStorageKey = Guid.NewGuid().ToString();
                        KeyValueStoreService.SetValueComposable(context, recommendationStorageKey, MortgageLoanStandardCreditRecommendationModel.KeyValueStoreKeySpace, JsonConvert.SerializeObject(recommendation));
                        AddDecisionItem("recommendationKeyValueItemKey", recommendationStorageKey, false);
                        if (recommendation.LeftToLiveOn.LtlAmount.HasValue)
                            AddDecisionItem("leftToLiveOnAmount", recommendation.LeftToLiveOn.LtlAmount.Value.ToString(CultureInfo.InvariantCulture), false);
                        if (recommendation.LoanToIncome.HasValue)
                            AddDecisionItem("loanToIncome", recommendation.LoanToIncome.Value.ToString(CultureInfo.InvariantCulture), false);
                        if (recommendation.LoanToValue.HasValue)
                            AddDecisionItem("loanToValue", recommendation.LoanToValue.Value.ToString(CultureInfo.InvariantCulture), false);
                        if (recommendation.PolicyFilterResult != null)
                        {
                            if (recommendation.PolicyFilterResult.IsAcceptRecommended.HasValue)
                                AddDecisionItem("isAcceptRecommended", recommendation.PolicyFilterResult.IsAcceptRecommended.Value ? "true" : "false", false);
                            if (recommendation.PolicyFilterResult.IsManualControlRecommended.HasValue)
                                AddDecisionItem("isManualControlRecommended", recommendation.PolicyFilterResult.IsManualControlRecommended.Value ? "true" : "false", false);
                        }
                    }

                    d.DecisionById = context.CurrentUserId;
                    d.DecisionDate = context.Clock.Now;
                    d.DecisionType = isFinal ? "Final" : "Initial";
                    AddDecisionItem("decisionType", d.DecisionType, false);
                    d.WasAutomated = wasAutomated;

                    AddDecisionItem("isOffer", isAccepted ? "true" : "false", false);
                    context.CreditDecisions.Add(d);

                    //Set as current decision
                    var h = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationNr);
                    d.CreditApplication = h;
                    h.CurrentCreditDecision = d;

                    //Change status
                    h.CreditCheckStatus = isAccepted ? CreditApplicationMarkerStatusName.Accepted : CreditApplicationMarkerStatusName.Rejected;

                    h.HideFromManualListsUntilDate = null;

                    string rejectionCommentPart = "";
                    string acceptCommentPart = "";
                    if (!isAccepted)
                    {
                        h.IsActive = false;
                        h.IsRejected = true;
                        h.RejectedById = ntechCurrentUserMetadata.UserId;
                        h.RejectedDate = clock.Now;
                    }

                    notificationData = GetDecisionNotificationDataIfPossible(
                        isAccepted ? null : rejection.RejectionReasons.Select(x => x.Code).ToHashSet(),
                        supressUserNotification, applicationNr, isFinal, out var emailNotificationFailedCommentPart);

                    var evt = context.CreateAndAddEvent(
                        isFinal
                            ? CreditApplicationEventCode.MortgageLoanStandardFinalCreditCheck
                            : CreditApplicationEventCode.MortgageLoanStandardInitialCreditCheck,
                        creditApplicationHeader: h);

                    //Write comment
                    var commentSuffix = (wasAutomated ? " (automated)" : "");
                    commentSuffix += emailNotificationFailedCommentPart ?? "";
                    context.CreateAndAddComment(
                        $"New credit check {(isAccepted ? $"accepted{acceptCommentPart}" : $"rejected{rejectionCommentPart}")}" + commentSuffix, evt.EventType, applicationNr: applicationNr);

                    workflowService.ChangeStepStatusComposable(context, (isFinal ? MortgageLoanStandardWorkflowService.FinalCreditCheckStep : MortgageLoanStandardWorkflowService.InitialCreditCheckStep).Name, isAccepted ? "Accepted" : "Rejected", application: h, evt: evt);

                    context.SaveChanges();

                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }

            if (notificationData != null)
            {
                SendDecisionNotification(notificationData);
            }

            return d;
        }
    }
}