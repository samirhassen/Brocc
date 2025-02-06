using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Banking.LoanModel;
using NTech.Banking.ScoringEngine;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateCompanyLoanInitialScoreMethod : TypedWebserviceMethod<CreateCompanyLoanInitialScoreMethod.Request, CreateCompanyLoanInitialScoreMethod.Response>
    {
        public override string Path => "CompanyLoan/Create-InitialScore";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            var f = new PluginScoringProcessFactory();

            var p = f.GetScoringProcess("CompanyLoanInitial");

            var ds = requestContext.Resolver().Resolve<IPluginScoringProcessDataSource>();

            try
            {
                var result = p.Score(request.ApplicationNr, ds);

                int repaymentTimeInMonths = 0;
                if (result.Offer != null)
                {
                    repaymentTimeInMonths = PaymentPlanCalculation
                        .BeginCreateWithAnnuity(result.Offer.LoanAmount, result.Offer.AnnuityAmount.Value, result.Offer.NominalInterestRatePercent, null, NEnv.CreditsUse360DayInterestYear)
                        .WithInitialFeeDrawnFromLoanAmount(result.Offer.InitialFeeAmount)
                        .WithMonthlyFee(result.Offer.MonthlyFeeAmount)
                        .EndCreate()
                        .Payments
                        .Count;
                }

                var response = new Response
                {
                    ApplicationNr = request.ApplicationNr,
                    WasAccepted = result.WasAccepted,
                    ManualAttentionRuleNames = result.ManualAttentionRuleNames?.ToList(),
                    RejectionRuleNames = result.RejectionRuleNames?.ToList(),
                    RiskClass = result.RiskClass,
                    ScorePointsByRuleName = result?.ScorePointsByRuleName,
                    DebugDataByRuleNames = result?.DebugDataByRuleNames,
                    ScoringData = result.ScoringData,
                    Offer = result.Offer == null ? null : new CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel
                    {
                        InitialFeeAmount = result.Offer.InitialFeeAmount,
                        LoanAmount = result.Offer.LoanAmount,
                        AnnuityAmount = result.Offer.AnnuityAmount.Value,
                        MonthlyFeeAmount = result.Offer.MonthlyFeeAmount,
                        NominalInterestRatePercent = result.Offer.NominalInterestRatePercent,
                        ReferenceInterestRatePercent = requestContext.Resolver().Resolve<ICreditClient>().GetCurrentReferenceInterest(),
                        RepaymentTimeInMonths = repaymentTimeInMonths
                    },
                    HistoricalApplications = result.HistoricalApplications,
                    HistoricalCredits = result.HistoricalCredits
                };

                if (request.StoreTempCopyOnServer)
                {
                    response.TempCopyStorageKey = requestContext.Resolver().Resolve<IEncryptedTemporaryStorageService>().StoreString(
                        JsonConvert.SerializeObject((CompanyLoanInitialCreditDecisionRecommendationModel)response), TimeSpan.FromHours(24));
                }

                return response;
            }
            catch (ServiceException e)
            {
                if (e.IsUserSafeException)
                    return Error(e.Message, errorCode: e.ErrorCode);
                else
                    throw;
            }
            catch (MissingRequiredScoringDataException e)
            {
                return Error($"The required scoring variable {e.ItemName} is missing", errorCode: "missingRequiredScoringData");
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public bool StoreTempCopyOnServer { get; set; }
        }

        public class Response : CompanyLoanInitialCreditDecisionRecommendationModel
        {
            public string TempCopyStorageKey { get; set; }
        }
    }
}