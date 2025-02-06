using Newtonsoft.Json;
using nPreCredit.Code.Email;
using NTech;
using NTech.Banking.LoanModel;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanCreditCheckService : ICompanyLoanCreditCheckService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IPartialCreditApplicationModelRepository creditApplicationModelRepository;
        private readonly IApplicationCommentService applicationCommentService;
        private readonly IServiceRegistryUrlService urlService;
        private readonly ICompanyLoanWorkflowService companyLoanWorkflowService;
        private readonly IClock clock;

        public CompanyLoanCreditCheckService(IClock clock, INTechCurrentUserMetadata ntechCurrentUserMetadata, IPartialCreditApplicationModelRepository creditApplicationModelRepository, IApplicationCommentService applicationCommentService, IServiceRegistryUrlService urlService, ICompanyLoanWorkflowService companyLoanWorkflowService)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.creditApplicationModelRepository = creditApplicationModelRepository;
            this.applicationCommentService = applicationCommentService;
            this.urlService = urlService;
            this.companyLoanWorkflowService = companyLoanWorkflowService;
            this.clock = clock;
        }

        public CreditDecision AcceptInitialCreditDecision(CompanyLoanInitialCreditDecisionRecommendationModel recommendation,
            CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel companyLoanOffer,
            bool wasAutomated)
        {
            return CommitInitialCreditDecision(recommendation, companyLoanOffer, null, wasAutomated, false);
        }

        public CreditDecision RejectInitialCreditDecision(CompanyLoanInitialCreditDecisionRecommendationModel recommendation,
            List<string> rejectionReasons,
            bool wasAutomated,
            bool supressRejectionNotification)
        {
            return CommitInitialCreditDecision(recommendation, null, rejectionReasons, wasAutomated, supressRejectionNotification);
        }

        private CreditDecision CommitInitialCreditDecision(
            CompanyLoanInitialCreditDecisionRecommendationModel recommendation,
            CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel companyLoanOffer,
            List<string> rejectionReasons,
            bool wasAutomated,
            bool supressUserNotification)
        {
            var isAccepted = companyLoanOffer != null;
            var applicationNr = recommendation.ApplicationNr;
            if (isAccepted)
            {
                if (companyLoanOffer.RepaymentTimeInMonths.HasValue && !companyLoanOffer.AnnuityAmount.HasValue)
                {
                    //Compute annuity
                    companyLoanOffer.AnnuityAmount = PaymentPlanCalculation
                        .BeginCreateWithRepaymentTime(
                                companyLoanOffer.LoanAmount.Value,
                                companyLoanOffer.RepaymentTimeInMonths.Value,
                                companyLoanOffer.NominalInterestRatePercent.Value + companyLoanOffer.ReferenceInterestRatePercent.GetValueOrDefault(), true, null, NEnv.CreditsUse360DayInterestYear)
                            .WithInitialFeeDrawnFromLoanAmount(companyLoanOffer.InitialFeeAmount.Value)
                            .WithMonthlyFee(companyLoanOffer.MonthlyFeeAmount.Value)
                            .EndCreate()
                            .AnnuityAmount;
                }
                else if (!companyLoanOffer.RepaymentTimeInMonths.HasValue && companyLoanOffer.AnnuityAmount.HasValue)
                {
                    //Compute repayment time
                    companyLoanOffer.RepaymentTimeInMonths = PaymentPlanCalculation
                        .BeginCreateWithAnnuity(
                                companyLoanOffer.LoanAmount.Value,
                                companyLoanOffer.AnnuityAmount.Value,
                                companyLoanOffer.NominalInterestRatePercent.Value + companyLoanOffer.ReferenceInterestRatePercent.GetValueOrDefault(),
                                null, NEnv.CreditsUse360DayInterestYear)
                            .WithInitialFeeDrawnFromLoanAmount(companyLoanOffer.InitialFeeAmount.Value)
                            .WithMonthlyFee(companyLoanOffer.MonthlyFeeAmount.Value)
                            .EndCreate()
                            .Payments
                            .Count;
                }
                else
                    throw new NTechWebserviceMethodException("Exactly one of AnnuityAmount and RepaymentTimeInMonths must be set on the AcceptedOffer")
                    {
                        IsUserFacing = true,
                        ErrorCode = "invalidOffer",
                        ErrorHttpStatusCode = 400
                    };
            }

            CreditDecision d;
            var rejectionEmailAddresses = new List<string>();
            var additionalQuestionsEmailAddresses = new List<string>();
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                context.BeginTransaction();
                try
                {
                    var cd = new CompanyLoanCreditDecisionModel
                    {
                        WasAccepted = isAccepted,
                        Recommendation = recommendation,
                        ScoringPass = "Initial"
                    };
                    Action<CreditDecision> fillDecision = x =>
                    {
                        x.ApplicationNr = recommendation.ApplicationNr;
                        x.DecisionById = context.CurrentUserId;
                        x.DecisionDate = context.Clock.Today;
                        x.DecisionType = cd.ScoringPass;
                        x.WasAutomated = wasAutomated;
                        context.FillInfrastructureFields(x);
                    };
                    if (isAccepted)
                    {
                        cd.CompanyLoanOffer = companyLoanOffer;
                        d = FillDecision(new AcceptedCreditDecision
                        {
                            AcceptedDecisionModel = JsonConvert.SerializeObject(cd),
                        }, fillDecision);
                    }
                    else
                    {
                        cd.RejectionReasons = rejectionReasons;
                        d = FillDecision(new RejectedCreditDecision
                        {
                            RejectedDecisionModel = JsonConvert.SerializeObject(cd),
                        }, fillDecision);
                    }
                    context.CreditDecisions.Add(d);

                    //Set as current decision
                    var h = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationNr);
                    h.CurrentCreditDecision = d;

                    //Change status
                    h.CreditCheckStatus = isAccepted ? CreditApplicationMarkerStatusName.Accepted : CreditApplicationMarkerStatusName.Rejected;

                    h.HideFromManualListsUntilDate = null;

                    var isSendingRejectionEmails = !isAccepted && !supressUserNotification && NEnv.GetAffiliateModel(h.ProviderName).IsSendingRejectionEmails;

                    var requiredApplicationItems = new List<string>() { "applicantCustomerId", "companyCustomerId", "applicantEmail", "additionalQuestionsAnswerDate" };

                    var app = new Lazy<PartialCreditApplicationModel>(() => creditApplicationModelRepository.Get(applicationNr, applicationFields: requiredApplicationItems));

                    string rejectionCommentPart = "";
                    string acceptCommentPart = "";
                    if (!isAccepted)
                    {
                        var rejectionReasonsSetup = CompanyLoanRejectionScoringSetup.Instance;

                        //Add search terms to decision
                        Code.CreditCheckCompletionProviderApplicationUpdater.AddRejectionReasonSearchTerms(rejectionReasons, rejectionReasonsSetup.GetIsKnownRejectionReason(), d, context);

                        //Add pause items to decision
                        var customerIds = new List<int>
                    {
                        app.Value.Application.Get("applicantCustomerId").IntValue.Required,
                        app.Value.Application.Get("companyCustomerId").IntValue.Required
                    };
                        var getPauseDaysByRejectionReason = rejectionReasonsSetup.GetRejectionReasonToPauseDaysMapping();
                        Code.CreditCheckCompletionProviderApplicationUpdater.AddRejectionReasonPauseDayItems(rejectionReasons, getPauseDaysByRejectionReason, customerIds.ToHashSet(), context, d);

                        h.IsActive = false;
                        h.IsRejected = true;
                        h.RejectedById = ntechCurrentUserMetadata.UserId;
                        h.RejectedDate = clock.Now;

                        if (isSendingRejectionEmails)
                        {
                            var applicantEmail = app.Value.Application.Get("applicantEmail").StringValue.Optional;
                            if (string.IsNullOrWhiteSpace(applicantEmail))
                            {
                                rejectionCommentPart = " (no rejection email will be sent since no applicant email was found)";
                            }
                            else
                            {
                                rejectionCommentPart = " (rejection email will be sent)";
                                rejectionEmailAddresses.Add(applicantEmail);
                            }
                        }
                    }
                    else
                    {
                        var applicantEmail = app.Value.Application.Get("applicantEmail").StringValue.Optional;
                        if (string.IsNullOrWhiteSpace(applicantEmail))
                        {
                            acceptCommentPart = " (no additional questions email will be sent since no applicant email was found)";
                        }
                        else
                        {
                            acceptCommentPart = " (additional questions email will be sent)";
                            additionalQuestionsEmailAddresses.Add(applicantEmail);
                        }
                    }

                    var evt = context.CreateAndAddEvent(CreditApplicationEventCode.CompanyLoanInitialCreditCheck, creditApplicationHeader: h);

                    //Write comment
                    var automationCommentSuffix = (wasAutomated ? " (automated)" : "");
                    context.CreateAndAddComment(
                        $"New initial credit check {(isAccepted ? $"accepted{acceptCommentPart}" : $"rejected{rejectionCommentPart}")}" + automationCommentSuffix, evt.EventType, applicationNr: applicationNr);

                    companyLoanWorkflowService.ChangeStepStatusComposable(context, "CreditCheck", isAccepted ? "Accepted" : "Rejected", application: h, evt: evt);

                    var additionalQuestionsAnswerDate = app.Value.Application.Get("additionalQuestionsAnswerDate").StringValue.Optional;
                    if (additionalQuestionsAnswerDate != null && additionalQuestionsAnswerDate != "pending")
                    {
                        //Reset additional questions status
                        context.AddOrUpdateCreditApplicationItems(h, new List<PreCreditContextExtended.CreditApplicationItemModel>
                    {
                        new PreCreditContextExtended.CreditApplicationItemModel { GroupName = "application", Name = "additionalQuestionsAnswerDate", Value = "pending", IsEncrypted = false }
                    }, "initialCreditCheck");
                    }

                    context.SaveChanges();

                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }

            if (!isAccepted && rejectionEmailAddresses.Any())
            {
                SendRejectionEmails(rejectionEmailAddresses, rejectionReasons, applicationNr);
            }
            else if (isAccepted && additionalQuestionsEmailAddresses.Any())
            {
                SendAdditionalQuestionsEmails(additionalQuestionsEmailAddresses, applicationNr);
            }

            return d;
        }

        private T FillDecision<T>(T d, Action<CreditDecision> f) where T : CreditDecision
        {
            //This exists only becase lambdas cannot have type parameters
            f(d);
            return d;
        }

        private void SendRejectionEmails(List<string> emails, List<string> creditCheckRejectionReasons, string applicationNr)
        {
            try
            {
                var templateName = CompanyLoanRejectionScoringSetup.Instance.GetRejectionEmailTemplateNameByRejectionReasons(creditCheckRejectionReasons);
                if (templateName != null)
                {
                    var s = EmailServiceFactory.CreateEmailService();
                    s.SendTemplateEmail(emails, templateName, null, $"Reason=CreditRejection, ApplicationNr={applicationNr}");
                }
                else
                {
                    Log.Debug("Rejection email not sent for {ApplicationNr} since no template matches the current set of rejection reasons", applicationNr);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Company loan: Failed to send rejection email");
                applicationCommentService.TryAddComment(applicationNr, "Failed to send rejection email. See error log for details.", "failedToSendRejectionEmail", null, out var _);
            }
        }

        private void SendAdditionalQuestionsEmails(List<string> emails, string applicationNr)
        {
            try
            {
                var link = this.urlService.ServiceRegistry.ExternalServiceUrl("nCustomerPages", $"a/#/q-eid-login/{applicationNr}/start").ToString();
                var s = EmailServiceFactory.CreateEmailService();
                s.SendTemplateEmail(emails,
                    "credit-additionalquestions",
                    new Dictionary<string, string> { { "link", link } },
                    $"Reason=CreditCheckAccepted, ApplicationNr={applicationNr}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Company loan: Failed to send additional questions email");
                applicationCommentService.TryAddComment(applicationNr, "Failed to send additional questions email. See error log for details.", "failedToSendAdditionalQuestionsEmail", null, out var _);
            }
        }
    }

    public interface ICompanyLoanCreditCheckService
    {
        CreditDecision AcceptInitialCreditDecision(CompanyLoanInitialCreditDecisionRecommendationModel recommendation,
            CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel companyLoanOffer, bool wasAutomated);

        CreditDecision RejectInitialCreditDecision(CompanyLoanInitialCreditDecisionRecommendationModel recommendation,
            List<string> rejectionReasons, bool wasAutomated, bool supressRejectionNotification);
    }
}