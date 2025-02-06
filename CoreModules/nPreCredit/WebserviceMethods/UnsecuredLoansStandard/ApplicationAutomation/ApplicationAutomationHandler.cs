using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared.Services.UlStandard.ApplicationAutomation;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation
{
    public class ApplicationAutomationHandler : EventSubscriberBase
    {
        private readonly ApplicationInfoModel _applicationInfoModel;
        private readonly ICustomerClient _customerClient;
        private readonly nPreCreditClient _preCreditClient;
        private readonly UnsecuredLoanStandardWorkflowService _wfService;
        private readonly UnsecuredLoanStandardAgreementService _agreementService;

        public ApplicationAutomationHandler(ApplicationInfoModel applicationInfoModel, UnsecuredLoanStandardWorkflowService wfService, UnsecuredLoanStandardAgreementService agreementService)
        {
            _customerClient = new PreCreditCustomerClient();
            _preCreditClient = new nPreCreditClient(AquireBearerToken);
            _applicationInfoModel = applicationInfoModel;
            _wfService = wfService;
            _agreementService = agreementService;
        }

        public void HandleCustomerDecisionAutomation(string cd, PreCreditContextExtended context, string whoChanged, IApplicationCancellationService applicationCancellationService)
        {
            var (isAcceptAutomation, isRejectAutomation) = ShouldAutoApproveCustomerDecision();

            if (!isAcceptAutomation && !isRejectAutomation)
                return;

            var isApplicantChange = whoChanged.StartsWith("Applicant");
            var customerCreditDecisionAutomatisation = new CreditDecisionStepAutomation(_wfService, applicationCancellationService, context);

            if (cd == "accepted" && isAcceptAutomation && isApplicantChange)
            {
                if (!customerCreditDecisionAutomatisation.TryAutomaticallyApproveOnCustomerAcceptsOffer(_applicationInfoModel.ApplicationNr))
                    context.CreateAndAddComment($"Could not automatically approve customer accept. Manual: {whoChanged} has {cd} offer", "CustomerDecisionCodeChanged", _applicationInfoModel.ApplicationNr);
            }

            if (cd == "rejected" && isRejectAutomation && isApplicantChange)
            {
                if (!customerCreditDecisionAutomatisation.TryAutomaticallyCancelOnCustomerRejectsOffer(_applicationInfoModel.ApplicationNr))
                    context.CreateAndAddComment($"Could not automatically cancel application. Manual: {whoChanged} has {cd} offer", "CustomerDecisionCodeChanged", _applicationInfoModel.ApplicationNr);
            }
        }

        public void HandleCustomerKycAutomation(List<int> customerIds, DateTime today)
        {
            if (!ShouldAutoApproveKyc())
                return;

            var kycStepAutomation = new KycStepAutomation(
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry),
                LegacyServiceClientFactory.CreatePreCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry),
                SerilogLoggingService.SharedInstance);
            if (kycStepAutomation.TryHandleKycStepAutomation(_applicationInfoModel.ApplicationNr, customerIds, today, out bool isApproved))
            {
                if (isApproved)
                {
                    HandlePostAutomationEvents(StepWithAutomationName.Kyc);
                }
            }
        }

        public void HandleCustomerFraudAutomation()
        {
            if (!ShouldAutoApproveFraud())
                return;

            var fraudAutomationHandler = new FraudStepAutomation(_applicationInfoModel, _wfService);
            if (fraudAutomationHandler.TryHandleFraudStepAutomation(_applicationInfoModel.ApplicationNr, out bool isApproved))
            {
                if (isApproved)
                {
                    HandlePostAutomationEvents(StepWithAutomationName.Fraud);
                }
            }
        }

        public bool TryHandleCustomerSignsAgreementAutomation()
        {
            try
            {
                if (ShouldAutoApproveAgreement())
                {
                    _preCreditClient.UnsecuredLoanStandardApproveAgreementStep(_applicationInfoModel.ApplicationNr, isApproved: true, isAutomatic: true);
                }

                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error when trying to handle application agreement step automation.");
                return false;
            }
        }

        private bool ShouldAutoApproveKyc()
        {
            var settings = _customerClient.LoadSettings("applicationAutomation");
            return settings["kycChecksOkApproveStep"] == "true";
        }

        public bool ShouldAutoApproveFraud()
        {
            var settings = _customerClient.LoadSettings("applicationAutomation");
            return settings["fraudChecksOkApproveStep"] == "true";
        }

        private (bool, bool) ShouldAutoApproveCustomerDecision()
        {
            var settings = _customerClient.LoadSettings("applicationAutomation");

            bool isAcceptAutomation = settings["customerAcceptsOfferApproveStep"] == "true";
            bool isRejectAutomation = settings["customerRejectsOfferCancelApplication"] == "true";

            return (isAcceptAutomation, isRejectAutomation);
        }

        public bool ShouldAutoSendAgreement()
        {
            var settings = _customerClient.LoadSettings("applicationAutomation");
            return settings["agreementCreatedSendOutForSigning"] == "true";
        }

        public bool ShouldAutoApproveAgreement()
        {
            var settings = _customerClient.LoadSettings("applicationAutomation");
            return settings["agreementSignedApproveStep"] == "true";
        }

        //Handles the rest of the automatized workflow after current automatized run 
        public void HandlePostAutomationEvents(StepWithAutomationName currentStep)
        {
            if (currentStep == StepWithAutomationName.Kyc)
            {
                if (ShouldAutoApproveFraud())
                {
                    var fraudAutomationHandler = new FraudStepAutomation(_applicationInfoModel, _wfService);
                    if (fraudAutomationHandler.TryApproveFraudStepAfterKycCheck(_applicationInfoModel.ApplicationNr))
                    {
                        if (ShouldAutoSendAgreement())
                        {
                            AgreementStepAutomation.TryCreateAndSendAgreement(_applicationInfoModel, _agreementService, _preCreditClient);
                        }
                    }
                }
            }

            if (currentStep == StepWithAutomationName.Fraud)
            {
                if (ShouldAutoSendAgreement())
                {
                    AgreementStepAutomation.TryCreateAndSendAgreement(_applicationInfoModel, _agreementService, _preCreditClient);
                }
            }
        }
    }

    public enum StepWithAutomationName
    {
        CustomerOfferDecision,
        Kyc,
        Fraud,
        Agreement
    }
}