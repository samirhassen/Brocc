using Newtonsoft.Json;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CreditRecommendationUlStandardService : LoanStandardCreditCheckBaseService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly UnsecuredLoanStandardWorkflowService workflowService;
        private readonly IReferenceInterestRateService referenceInterestRateService;
        private readonly ICoreClock clock;
        private readonly IPartialCreditApplicationModelRepository partialRepository;
        private readonly IApplicationCommentService applicationCommentService;
        private readonly IClientConfigurationCore clientConfiguration;

        public CreditRecommendationUlStandardService(ICoreClock clock, INTechCurrentUserMetadata ntechCurrentUserMetadata, UnsecuredLoanStandardWorkflowService workflowService, IReferenceInterestRateService referenceInterestRateService,
            IPartialCreditApplicationModelRepository pcamRepository, IApplicationCommentService applicationCommentService, IPreCreditEnvSettings envSettings, IPreCreditContextFactoryService preCreditContextFactory,
            ICustomerClient customerClient, IMarkdownTemplateRenderingService templateRenderingService, INTechEmailServiceFactory emailServiceFactory, LoanStandardEmailTemplateService emailTemplateService,
            INTechServiceRegistry serviceRegistry, IClientConfigurationCore clientConfiguration) : base(applicationCommentService, envSettings, preCreditContextFactory, customerClient, templateRenderingService, emailServiceFactory, emailTemplateService, serviceRegistry)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.workflowService = workflowService;
            this.referenceInterestRateService = referenceInterestRateService;
            this.clock = clock;
            partialRepository = pcamRepository;
            this.applicationCommentService = applicationCommentService;
            this.clientConfiguration = clientConfiguration;
        }

        public CreditDecision AcceptInitialCreditDecision(string applicationNr, UnsecuredLoanStandardCurrentCreditDecisionOfferModel offer, bool wasAutomated, bool supressOfferNotification, UnsecuredLoanStandardCreditRecommendationModel recommendation)
        {
            return CommitInitialCreditDecision(applicationNr, offer, null, wasAutomated, supressOfferNotification, recommendation);
        }

        public CreditDecision RejectInitialCreditDecision(string applicationNr, UnsecuredLoanStandardCurrentCreditDecisionRejectionModel rejection, bool wasAutomated, bool supressRejectionNotification, UnsecuredLoanStandardCreditRecommendationModel recommendation)
        {
            return CommitInitialCreditDecision(applicationNr, null, rejection, wasAutomated, supressRejectionNotification, recommendation);
        }

        private CreditDecision CommitInitialCreditDecision(
            string applicationNr,
            UnsecuredLoanStandardCurrentCreditDecisionOfferModel offer,
            UnsecuredLoanStandardCurrentCreditDecisionRejectionModel rejection,
            bool wasAutomated,
            bool supressUserNotification,
            UnsecuredLoanStandardCreditRecommendationModel recommendation)
        {
            CreditDecision d;
            DecisionNotificationData notificationData;                        

            void AddDecisionItem(string itemName, string value, bool isRepeatable) =>
                d.DecisionItems.Add(new CreditDecisionItem { IsRepeatable = isRepeatable, ItemName = itemName, Value = value });

            using (var context = preCreditContextFactory.CreateExtended())
            {
                context.BeginTransaction();
                try
                {
                    var isAccepted = offer != null;

                    if (isAccepted)
                    {
                        var totalFirstNotificationCostAmount = offer.FirstNotificationCosts?.Sum(x => x.Value ?? 0m) ?? 0m;

                        PaymentPlanCalculation plan;
                        if (!offer.ReferenceInterestRatePercent.HasValue)
                            offer.ReferenceInterestRatePercent = referenceInterestRateService.GetCurrent();

                        var totalInterestRatePercent = offer.NominalInterestRatePercent.Value + offer.ReferenceInterestRatePercent.Value;
                        if(offer.SinglePaymentLoanRepaymentTimeInDays.HasValue)
                        {
                            plan = PaymentPlanCalculation.CaclculateSinglePaymentWithRepaymentTimeInDays(
                                offer.ComputeLoanAmount(),
                                offer.SinglePaymentLoanRepaymentTimeInDays.Value,
                                totalInterestRatePercent,
                                initialFeeOnNotification: totalFirstNotificationCostAmount,
                                notificationFee: offer.NotificationFeeAmount);
                        }
                        else  if (offer.RepaymentTimeInMonths.HasValue)
                        {
                            plan = PaymentPlanCalculation.BeginCreateWithRepaymentTime(
                                    offer.ComputeLoanAmount(),
                                    offer.RepaymentTimeInMonths.Value,
                                    totalInterestRatePercent,
                                    true, null, envSettings.CreditsUse360DayInterestYear)
                                .WithInitialFeeCapitalized(offer.InitialFeeCapitalizedAmount ?? 0m)
                                .WithInitialFeeDrawnFromLoanAmount(offer.InitialFeeDrawnFromLoanAmount ?? 0m)
                                .WithMonthlyFee(offer.NotificationFeeAmount ?? 0m)
                                .WithInitialFeePaidOnFirstNotification(totalFirstNotificationCostAmount)
                                .EndCreate();
                            if (!offer.AnnuityAmount.HasValue)
                                offer.AnnuityAmount = plan.AnnuityAmount;
                        }
                        else if (!offer.RepaymentTimeInMonths.HasValue && offer.AnnuityAmount.HasValue)
                        {
                            plan = PaymentPlanCalculation.BeginCreateWithAnnuity(
                                    offer.ComputeLoanAmount(),
                                    offer.AnnuityAmount.Value,
                                    totalInterestRatePercent,
                                    null, envSettings.CreditsUse360DayInterestYear)
                                .WithInitialFeeCapitalized(offer.InitialFeeCapitalizedAmount ?? 0m)
                                .WithInitialFeeDrawnFromLoanAmount(offer.InitialFeeDrawnFromLoanAmount ?? 0m)
                                .WithInitialFeePaidOnFirstNotification(totalFirstNotificationCostAmount)
                                .WithMonthlyFee(offer.NotificationFeeAmount ?? 0m)
                                .EndCreate();
                            offer.RepaymentTimeInMonths = plan.Payments.Count;
                        }
                        else
                            throw new NTechCoreWebserviceException("Exactly one of AnnuityAmount and RepaymentTimeInMonths must be set on the AcceptedOffer")
                            {
                                IsUserFacing = true,
                                ErrorCode = "invalidOffer",
                                ErrorHttpStatusCode = 400
                            };

                        d = context.FillInfrastructureFields(new AcceptedCreditDecision { AcceptedDecisionModel = DecisionModelMarker.Value, DecisionItems = new List<CreditDecisionItem>() });

                        AddDecisionItem("initialFeeCapitalizedAmount", offer.InitialFeeCapitalizedAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("loansToSettleAmount", offer.SettleOtherLoansAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("initialFeeWithheldAmount", offer.InitialFeeDrawnFromLoanAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("paidToCustomerAmount", offer.PaidToCustomerAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);

                        if(plan.Payments.Count == 1 && plan.Payments[0].NonStandardPaymentDays.HasValue)
                            AddDecisionItem("singlePaymentLoanRepaymentTimeInDays", plan.Payments[0].NonStandardPaymentDays.Value.ToString(CultureInfo.InvariantCulture), false);
                        else
                            AddDecisionItem("repaymentTimeInMonths", offer.RepaymentTimeInMonths.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);

                        AddDecisionItem("loanAmount", offer.ComputeLoanAmount().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("notificationFeeAmount", offer.NotificationFeeAmount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("marginInterestRatePercent", offer.NominalInterestRatePercent.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("referenceInterestRatePercent", offer.ReferenceInterestRatePercent.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("totalInterestRatePercent", totalInterestRatePercent.ToString(CultureInfo.InvariantCulture), false);

                        if (plan.UsesAnnuities)
                            AddDecisionItem("annuityAmount", plan.AnnuityAmount.ToString(CultureInfo.InvariantCulture), false);
                        else
                            AddDecisionItem("fixedMonthlyCapitalAmount", plan.FixedMonthlyCapitalAmount.ToString(CultureInfo.InvariantCulture), false);

                        AddDecisionItem("totalPaidAmount", plan.TotalPaidAmount.ToString(CultureInfo.InvariantCulture), false);
                        AddDecisionItem("effectiveInterestRatePercent", plan.EffectiveInterestRatePercent.Value.ToString(CultureInfo.InvariantCulture), false);

                        foreach (var cost in offer.FirstNotificationCosts ?? Enumerable.Empty<UnsecuredLoanStandardCurrentCreditDecisionOfferFirstNotificationCostItem>())
                        {
                            if (cost.Value > 0m)
                                AddDecisionItem(FormatFirstNotificationCostAmountDecisionItem(cost.Code), cost.Value.Value.ToString(CultureInfo.InvariantCulture), false);
                        }

                        var limitEngine = new HandlerLimitEngine(preCreditContextFactory, clientConfiguration);
                        limitEngine.CheckHandlerLimits(offer.ComputeLoanAmount(), 0, ntechCurrentUserMetadata.UserId,
                            out var isOverHandlerLimit, out var isAllowedToOverrideHandlerLimit);

                        if (isOverHandlerLimit)
                        {
                            if (isAllowedToOverrideHandlerLimit.HasValue && isAllowedToOverrideHandlerLimit.Value)
                            {
                                AddDecisionItem("handlerLimitWasOverridden", "true", false);
                            }
                            else
                            {
                                throw new NTechCoreWebserviceException("Offer amount over handler limit and not allowed to override. ")
                                {
                                    IsUserFacing = true,
                                    ErrorCode = "overHandlerLimit",
                                    ErrorHttpStatusCode = 400
                                };
                            }
                        }
                    }
                    else
                    {
                        d = context.FillInfrastructureFields(new RejectedCreditDecision { RejectedDecisionModel = DecisionModelMarker.Value, DecisionItems = new List<CreditDecisionItem>() });
                        foreach (var rejectionReason in rejection.RejectionReasons)
                        {
                            AddDecisionItem("rejectionReason", rejectionReason.Code, true);
                            AddDecisionItem("rejectionReasonText", rejectionReason.DisplayName, true);
                        }
                        if (!string.IsNullOrWhiteSpace(rejection.OtherText))
                            AddDecisionItem("decisionType", rejection.OtherText, false);
                    }

                    if (recommendation != null)
                    {
                        var recommendationStorageKey = Guid.NewGuid().ToString();
                        KeyValueStoreService.SetValueComposable(context, recommendationStorageKey, UnsecuredLoanStandardCreditRecommendationModel.KeyValueStoreKeySpace, JsonConvert.SerializeObject(recommendation));
                        AddDecisionItem("recommendationKeyValueItemKey", recommendationStorageKey, false);
                        if (recommendation.LeftToLiveOn.LtlAmount.HasValue)
                            AddDecisionItem("leftToLiveOnAmount", recommendation.LeftToLiveOn.LtlAmount.Value.ToString(CultureInfo.InvariantCulture), false);
                        if (recommendation.ProbabilityOfDefaultPercent.HasValue)
                            AddDecisionItem("probabilityOfDefaultPercent", recommendation.ProbabilityOfDefaultPercent.Value.ToString(CultureInfo.InvariantCulture), false);
                        if (recommendation.DebtBurdenRatio.HasValue)
                            AddDecisionItem("debtBurdenRatio", recommendation.DebtBurdenRatio.Value.ToString(CultureInfo.InvariantCulture), false);
                        if (recommendation.PolicyFilterResult != null)
                        {
                            if (recommendation.PolicyFilterResult.IsAcceptRecommended.HasValue)
                                AddDecisionItem("isAcceptRecommended", recommendation.PolicyFilterResult.IsAcceptRecommended.Value ? "true" : "false", false);
                            if (recommendation.PolicyFilterResult.IsManualControlRecommended.HasValue)
                                AddDecisionItem("isManualControlRecommended", recommendation.PolicyFilterResult.IsManualControlRecommended.Value ? "true" : "false", false);
                        }
                    }

                    d.DecisionById = context.CurrentUserId;
                    d.DecisionDate = context.CoreClock.Now;
                    d.DecisionType = "Initial";
                    AddDecisionItem("decisionType", d.DecisionType, false);
                    d.WasAutomated = wasAutomated;

                    AddDecisionItem("customerDecisionCode", "initial", false);
                    AddDecisionItem("isOffer", isAccepted ? "true" : "false", false);
                    context.AddCreditDecisions(d);

                    //Set as current decision                    
                    var h = context.CreditApplicationHeadersWithItemsIncludedQueryable.Single(x => x.ApplicationNr == applicationNr);
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
                        isAccepted ? null : rejection.RejectionReasons.Select(x => x.Code).ToHashSetShared(),
                        supressUserNotification, applicationNr, false, out var emailNotificationFailedCommentPart);

                    var evt = context.CreateAndAddEvent(CreditApplicationEventCode.UnsecuredLoanStandardCreditCheck, null, h);

                    //Write comment
                    var commentSuffix = (wasAutomated ? " (automated)" : "");
                    commentSuffix += emailNotificationFailedCommentPart ?? "";
                    context.CreateAndAddComment(
                        $"New credit check {(isAccepted ? $"accepted{acceptCommentPart}" : $"rejected{rejectionCommentPart}")}" + commentSuffix, evt.EventType, applicationNr: applicationNr);

                    workflowService.ChangeStepStatusComposable(context, UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name, isAccepted ? "Accepted" : "Rejected", application: h, evt: evt);

                    context.SaveChanges();

                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }

            if (notificationData != null)
            {
                SendDecisionNotification(notificationData);
            }

            return d;
        }

        public static string FormatFirstNotificationCostAmountDecisionItem(string code) => $"firstNotificationCost_{code}_Amount";
        public static bool IsFirstNotificationCostAmountDecisionItem(string itemName) => itemName != null && itemName.StartsWith("firstNotificationCost_") && itemName.EndsWith("_Amount");
        public static string GetFirstNotificationCostAmountDecisionItemCode(string itemName)
        {
            if (!IsFirstNotificationCostAmountDecisionItem(itemName))
                throw new NTechCoreWebserviceException("Not a valid first notification cost item code");
            var codeAndSuffix = itemName.Substring("firstNotificationCost_".Length);
            return codeAndSuffix.Substring(0, codeAndSuffix.Length - "_Amount".Length);
        }

        public bool HasActiveCustomerDecision(string applicationNr)
        {
            using (var context = preCreditContextFactory.CreateExtended())
            {
                var decision = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        CustomerDecisionCode = x
                            .CurrentCreditDecision
                            .DecisionItems
                            .Where(y => y.ItemName == "customerDecisionCode")
                            .Select(y => y.Value)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault()?
                    .CustomerDecisionCode;
                return !(decision == null || decision == "initial");

            }
        }

        public static Lazy<string> DecisionModelMarker = new Lazy<string>(() => Newtonsoft.Json.JsonConvert.SerializeObject(new { usesItems = true }));
    }


    public class UnsecuredLoanStandardCurrentCreditDecisionOfferModel : IValidatableObject
    {
        public decimal? PaidToCustomerAmount { get; set; }
        public decimal? SettleOtherLoansAmount { get; set; }
        public decimal? AnnuityAmount { get; set; }
        public int? SinglePaymentLoanRepaymentTimeInDays { get; set; }
        public int? RepaymentTimeInMonths { get; set; }
        [Required]
        public decimal? NominalInterestRatePercent { get; set; }
        public decimal? ReferenceInterestRatePercent { get; set; }
        public decimal? NotificationFeeAmount { get; set; }
        public decimal? InitialFeeCapitalizedAmount { get; set; }
        public decimal? InitialFeeDrawnFromLoanAmount { get; set; }
        public List<UnsecuredLoanStandardCurrentCreditDecisionOfferFirstNotificationCostItem> FirstNotificationCosts { get; set; }

        public decimal ComputeLoanAmount() =>
            PaidToCustomerAmount.GetValueOrDefault()
            + SettleOtherLoansAmount.GetValueOrDefault()
            + InitialFeeDrawnFromLoanAmount.GetValueOrDefault();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(SinglePaymentLoanRepaymentTimeInDays.HasValue)
            {
                if (AnnuityAmount.HasValue || RepaymentTimeInMonths.HasValue)
                    yield return new ValidationResult("AnnuityAmount and RepaymentTimeInMonths cannot be combined with SinglePaymentLoanRepaymentTimeInDays");
            }
            else if (!AnnuityAmount.HasValue && !RepaymentTimeInMonths.HasValue)
                yield return new ValidationResult("At least one of AnnuityAmount and RepaymentTimeInMonths required");
        }
    }

    public class UnsecuredLoanStandardCurrentCreditDecisionOfferFirstNotificationCostItem
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public decimal? Value { get; set; }
    }

    public class UnsecuredLoanStandardCurrentCreditDecisionRejectionModel
    {
        [Required]
        public List<UnsecuredLoanStandardCurrentCreditDecisionRejectionReasonModel> RejectionReasons { get; set; }
        public string OtherText { get; set; }
    }

    public class UnsecuredLoanStandardCurrentCreditDecisionRejectionReasonModel
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
    }
}