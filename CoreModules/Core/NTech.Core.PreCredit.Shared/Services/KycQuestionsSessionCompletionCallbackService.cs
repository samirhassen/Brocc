using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared.Services.UlStandard.ApplicationAutomation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class KycQuestionsSessionCompletionCallbackService
    {
        public const string UnsecuredLoanStandardCallbackType = "unsecuredLoanApplicationStandardV1";

        private readonly ICustomerClient customerClient;
        private readonly KycStepAutomation kycStepAutomation;
        private readonly ApplicationInfoService applicationInfoService;
        private readonly ICoreClock clock;

        public KycQuestionsSessionCompletionCallbackService(ICustomerClient customerClient, KycStepAutomation kycStepAutomation, ApplicationInfoService applicationInfoService, ICoreClock clock)
        {
            this.customerClient = customerClient;
            this.kycStepAutomation = kycStepAutomation;
            this.applicationInfoService = applicationInfoService;
            this.clock = clock;
        }

        public void HandleKycQuestionSessionCompleted(string sessionId)
        {
            var session = customerClient.FetchKycQuestionSession(sessionId);
            if (session?.IsCompleted != true)
                return;

            var callbackType = session.CustomData.Opt("callbackType");

            if (callbackType == UnsecuredLoanStandardCallbackType) 
            {
                var applicationNr = session.CustomData["applicationNr"];
                var customerIds = applicationInfoService.GetApplicationApplicants(applicationNr).CustomerIdByApplicantNr.Values;
                kycStepAutomation.TryHandleKycStepAutomation(applicationNr, customerIds.ToList(), clock.Today, out var _);
            }
        }
    }
}
