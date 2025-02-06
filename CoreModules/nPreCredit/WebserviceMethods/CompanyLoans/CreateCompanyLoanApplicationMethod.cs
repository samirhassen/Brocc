using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Plugins;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Banking.ScoringEngine;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateCompanyLoanApplicationMethod : RawRequestWebserviceMethod<CreateCompanyLoanApplicationMethod.Response>
    {
        public override string Path => "CompanyLoan/Create-Application";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        public override Type RequestType
        {
            get
            {
                return PluginCompanyLoanApplicationRequestTranslator.GetRequestType();
            }
        }

        protected override Response DoExecuteRaw(NTechWebserviceMethodRequestContext requestContext, string jsonRequest)
        {
            var request = JsonConvert.DeserializeObject(jsonRequest, RequestType);

            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var tr = resolver.Resolve<PluginCompanyLoanApplicationRequestTranslator>();
            if (!tr.TranslateApplicationRequest(request, out var internalRequest, out var errorCodeAndMessage))
                return Error(errorCodeAndMessage.Item2, errorCode: errorCodeAndMessage.Item1);


            var c = resolver.Resolve<SharedCreateApplicationService>();
            var wf = resolver.Resolve<ICompanyLoanWorkflowService>();

            var createApplicationResponse = c.CreateApplication(internalRequest,
                CreditApplicationTypeCode.companyLoan,
                wf,
                CreditApplicationEventCode.CompanyLoanApplicationCreated);

            var response = new Response
            {
                ApplicationNr = createApplicationResponse.ApplicationNr,
                DecisionStatus = "Pending"
            };

            var commentService = requestContext.Resolver().Resolve<IApplicationCommentService>();
            Action<string> addInitialScoringFailedComment = s => commentService
                        .TryAddComment(response.ApplicationNr, s, "initalAutomaticScoringFailed", null, out var _);

            var skipInitialScoring = internalRequest.SkipInitialScoring;
            if (!skipInitialScoring)
            {
                skipInitialScoring = requestContext.Resolver().Resolve<ApplicationCheckpointService>().DoesAnyApplicationHaveAnActiveCheckpoint(createApplicationResponse.ApplicationNr);
                if (skipInitialScoring)
                    commentService
                       .TryAddComment(response.ApplicationNr, "Automatic credit check skipped due to customer checkpoint", "initalAutomaticScoringCheckpoint", null, out var _);
            }

            if (!skipInitialScoring)
            {
                var f = new PluginScoringProcessFactory();

                var p = f.GetScoringProcess("CompanyLoanInitial");

                var ds = resolver.Resolve<IPluginScoringProcessDataSource>();
                var cs = resolver.Resolve<ICompanyLoanCreditCheckService>();

                try
                {
                    var scoringResult = p.Score(createApplicationResponse.ApplicationNr, ds);
                    var offer = scoringResult?.Offer;
                    var recommendation = new CompanyLoanInitialCreditDecisionRecommendationModel
                    {
                        ApplicationNr = createApplicationResponse.ApplicationNr,
                        HistoricalApplications = scoringResult.HistoricalApplications,
                        ManualAttentionRuleNames = scoringResult.ManualAttentionRuleNames?.ToList(),
                        WasAccepted = scoringResult.WasAccepted,
                        HistoricalCredits = scoringResult.HistoricalCredits,
                        RejectionRuleNames = scoringResult.RejectionRuleNames?.ToList(),
                        Offer = offer == null ? null : new CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel
                        {
                            AnnuityAmount = offer.AnnuityAmount,
                            InitialFeeAmount = offer.InitialFeeAmount,
                            LoanAmount = offer.LoanAmount,
                            MonthlyFeeAmount = offer.MonthlyFeeAmount,
                            NominalInterestRatePercent = offer.NominalInterestRatePercent,
                            ReferenceInterestRatePercent = requestContext.Resolver().Resolve<ICreditClient>().GetCurrentReferenceInterest()
                        },
                        RiskClass = scoringResult.RiskClass,
                        ScorePointsByRuleName = scoringResult.ScorePointsByRuleName,
                        DebugDataByRuleNames = scoringResult.DebugDataByRuleNames,
                        ScoringData = scoringResult.ScoringData
                    };

                    if (recommendation.ManualAttentionRuleNames?.Any() ?? false)
                    {
                        addInitialScoringFailedComment($"Automatic scoring {(recommendation.WasAccepted ? "accepted" : "rejected")} but requiring manual attention");
                    }
                    else if (recommendation.WasAccepted)
                    {
                        var decision = cs.AcceptInitialCreditDecision(recommendation, recommendation.Offer, true);
                        response.Offer = CompanyLoanExtendedOfferModel.CreateFromOffer(recommendation.Offer, NEnv.EnvSettings);
                        response.DecisionStatus = "Accepted";
                    }
                    else
                    {
                        var names = CompanyLoanRejectionScoringSetup.Instance.GetRejectionReasonNameByRuleName();
                        var rejectionReasons = recommendation.RejectionRuleNames.Select(x => names.Opt(x) ?? x).ToHashSet();
                        var decision = cs.RejectInitialCreditDecision(recommendation, rejectionReasons.ToList(), true, internalRequest.SupressUserNotification);
                        response.RejectionCodes = recommendation.RejectionRuleNames;
                        response.DecisionStatus = "Rejected";
                    }
                }
                catch (ServiceException e)
                {
                    var msg = e.IsUserSafeException ? e.Message : "See error log";
                    if (!e.IsUserSafeException)
                        Log.Error(e, "CompanyLoan/Create-Application");

                    addInitialScoringFailedComment($"Automatic scoring failed: {msg}");
                }
                catch (MissingRequiredScoringDataException e)
                {
                    addInitialScoringFailedComment($"Automatic scoring failed: {e.Message}");
                }
                catch (Exception e)
                {
                    addInitialScoringFailedComment($"Automatic scoring crashed: See error log");
                    Log.Error(e, "CompanyLoan/Create-Application");
                }
            }

            return response;
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public string DecisionStatus { get; set; }

            public CompanyLoanExtendedOfferModel Offer { get; set; }
            public List<string> RejectionCodes { get; set; }
        }
    }
}