using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared.Models;
using System;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateApplicationWithScoringUlStandardService
    {
        private readonly CreateApplicationUlStandardService createApplicationService;
        private readonly CachedSettingsService settingsService;
        private readonly NewCreditCheckUlStandardService creditCheckService;
        private readonly CreditRecommendationUlStandardService creditRecommendationUlStandardService;
        private readonly ILoggingService loggingService;

        public CreateApplicationWithScoringUlStandardService(CreateApplicationUlStandardService createApplicationService, CachedSettingsService settingsService, 
            NewCreditCheckUlStandardService creditCheckService, CreditRecommendationUlStandardService creditRecommendationUlStandardService, ILoggingService loggingService)
        {
            this.createApplicationService = createApplicationService;
            this.settingsService = settingsService;
            this.creditCheckService = creditCheckService;
            this.creditRecommendationUlStandardService = creditRecommendationUlStandardService;
            this.loggingService = loggingService;
        }

        public UlStandardApplicationResponse CreateApplicationWithAutomaticScoring(UlStandardApplicationRequest request, bool isFromInsecureSource, string requestJson)
        {
            var applicationNr = createApplicationService.CreateApplication(request, isFromInsecureSource, requestJson);

            var response = new UlStandardApplicationResponse
            {
                ApplicationNr = applicationNr
            };

            
            if (request?.Meta?.SkipInitialScoring != true && settingsService.LoadSettings("applicationAutomation").Opt("creditCheckRecommendedReject") == "true")
            {
                try
                {
                    var recommendation = creditCheckService.NewCreditCheck(applicationNr);
                    if (recommendation.PolicyFilterResult.IsAcceptRecommended == false)
                    {
                        var decision = creditRecommendationUlStandardService.RejectInitialCreditDecision(applicationNr,
                            new UnsecuredLoanStandardCurrentCreditDecisionRejectionModel
                            {
                                RejectionReasons = recommendation.PolicyFilterResult.RejectionReasonNames.Select(x => new UnsecuredLoanStandardCurrentCreditDecisionRejectionReasonModel
                                {
                                    Code = x,
                                    DisplayName = SplitAndCapitalize(x)
                                }).ToList()
                            }, wasAutomated: true, supressRejectionNotification: false, recommendation); //TODO: Should we supress the notification?
                        response.DecisionStatus = "Rejected";
                        response.RejectionCodes = recommendation.PolicyFilterResult.RejectionReasonNames.ToList();
                    }
                } 
                catch(Exception ex)
                {
                    loggingService.Error(ex, $"Automatic scoring failed for {applicationNr}");
                }
            }

            return response;
        }

        /// <summary>
        /// Splits something like 'theOtherThing' or 'TheOtherThing' into 'The other thing'
        /// </summary>
        private static string SplitAndCapitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
                return input;

            //Basically replace all inner uppercase chars with a space and that char in lowercase.
            return input.Substring(0, 1).ToUpperInvariant() +
                string.Join("", input.Skip(1).Select(x => ((char.IsUpper(x) ? " " : "") + $"{x}".ToLowerInvariant())));
        }
    }
}