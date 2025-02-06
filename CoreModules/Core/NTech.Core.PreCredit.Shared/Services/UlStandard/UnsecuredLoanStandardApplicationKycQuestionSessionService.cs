using nPreCredit.Code.Services;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services.UlStandard
{
    public class UnsecuredLoanStandardApplicationKycQuestionSessionService
    {
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly ApplicationInfoService applicationInfoService;
        private readonly IPreCreditContextFactoryService contextFactory;

        public UnsecuredLoanStandardApplicationKycQuestionSessionService(ICustomerClient customerClient, IClientConfigurationCore clientConfiguration,
            INTechServiceRegistry serviceRegistry, ApplicationInfoService applicationInfoService, IPreCreditContextFactoryService contextFactory)
        {
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
            this.serviceRegistry = serviceRegistry;
            this.applicationInfoService = applicationInfoService;
            this.contextFactory = contextFactory;
        }

        public KycQuestionsSession CreateSession(string applicationNr, int? customerId)
        {            
            var info = applicationInfoService.GetApplicationApplicants(applicationNr);
            if(info == null)
            {
                throw new NTechCoreWebserviceException("No such application exists") { ErrorHttpStatusCode = 400, IsUserFacing = true };
            }
            
            var customerIds = info.CustomerIdByApplicantNr.Values.ToList();
            if (customerId.HasValue && !customerIds.Contains(customerId.Value))
            {
                throw new NTechCoreWebserviceException("No such application exists") { ErrorHttpStatusCode = 400, IsUserFacing = true };
            }

            KycQuestionsSession session = null;
            using (var context = contextFactory.CreateExtended())
            {
                var applicationRow = ComplexApplicationListService.GetListRow(applicationNr, "Application", 1, context);
                var kycQuestionSessionId = applicationRow.UniqueItems.Opt("kycQuestionSessionId");
                if(kycQuestionSessionId != null)
                {
                    session = customerClient.FetchKycQuestionSession(kycQuestionSessionId);
                }
            }

            if(session == null || !session.IsActive)
            {
                session = customerClient.CreateKycQuestionSession(new CreateKycQuestionSessionRequest
                {
                    CustomerIds = customerIds,
                    Language = clientConfiguration.Country.GetBaseLanguage(),
                    QuestionsRelationType = "Credit_UnsecuredLoan",
                    RedirectUrl = serviceRegistry.ExternalServiceUrl("nCustomerPages", $"n/unsecured-loan-applications/application/{applicationNr}").ToString(),
                    SlidingExpirationHours = 4,
                    SourceType = "UnsecuredLoanApplication",
                    SourceId = applicationNr,
                    SourceDescription = $"Unsecured loan application {applicationNr}",
                    CompletionCallbackModuleName = "nPreCredit",
                    CustomData = new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["callbackType"] = KycQuestionsSessionCompletionCallbackService.UnsecuredLoanStandardCallbackType,
                        ["applicationNr"] = applicationNr
                    },
                    AllowBackToRedirectUrl = true
                });
                using (var context = contextFactory.CreateExtended())
                {
                    var applicationRow = ComplexApplicationListService.SetSingleUniqueItem(applicationNr, "Application", "kycQuestionSessionId", 1, session.SessionId, context);
                    context.SaveChanges();
                }
            }

            return session;
        }
    }
}
