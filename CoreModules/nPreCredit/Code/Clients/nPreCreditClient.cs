using nPreCredit.Controllers;
using NTech.Services.Infrastructure;
using System;

namespace nPreCredit.Code.Clients
{
    public class nPreCreditClient
    {
        private readonly Func<string> aquireBearerToken;

        public nPreCreditClient(Func<string> aquireBearerToken)
        {
            this.aquireBearerToken = aquireBearerToken;
        }

        protected NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nPreCredit"]), bearerToken ?? aquireBearerToken(), timeout: timeout);
        }

        public void SignalSessionEventAuthorized(string sessionId, string providerName)
        {
            Begin()
                .PostJson("api/Signatures/Signal-Session-Event-Authorized", new { sessionId, providerName })
                .EnsureSuccessStatusCode();
        }

        public void AutomaticCreditCheck(string applicationNr, bool followRejectRecommendation, bool followAcceptRecommendation)
        {
            Begin()
                .PostJson("api/creditapplication/creditcheck/automatic", new { applicationNr, followRejectRecommendation, followAcceptRecommendation })
                .EnsureSuccessStatusCode();
        }

        public void RejectCreditApplication(string applicationNr, bool wasAutomated)
        {
            Begin()
                .PostJson("CreditManagement/RejectApplication", new { applicationNr, wasAutomated })
                .EnsureSuccessStatusCode();
        }

        public void SignCallback(string id, bool success, string errorMessage = null)
        {
            Begin()
                .PostJson("api/additionalquestions/signcallback", new { id = id, success = success, errorMessage = errorMessage })
                .EnsureSuccessStatusCode();
        }

        public void AddCommentToApplication(string applicationNr, string commentText, string attachedFileAsDataUrl, string attachedFileName, string eventType)
        {
            Begin()
                .PostJson("CreditManagement/AddComment", new { applicationNr = applicationNr, commentText = commentText, attachedFileAsDataUrl = attachedFileAsDataUrl, attachedFileName = attachedFileName, eventType = eventType })
                .EnsureSuccessStatusCode();
        }

        public void CancelApplication(string applicationNr, CancelledByExternalRequestModel cancelledByExternalRequest)
        {
            Begin()
                .PostJson("CreditManagement/CancelApplication", new { applicationNr = applicationNr, cancelledByExternalRequest = cancelledByExternalRequest })
                .EnsureSuccessStatusCode();
        }

        public AffiliateReporting.ApplicationStateModel GetApplicationState(string applicationNr)
        {
            return Begin()
                .PostJson("api/providerreporting/applicationstate", new { applicationNr = applicationNr })
                .ParseJsonAs<AffiliateReporting.ApplicationStateModel>();
        }

        public void LoanStandardApproveKycStep(string applicationNr, bool isApproved, bool isAutomatic)
        {
            Begin()
                .PostJson("api/LoanStandard/Kyc/Set-Approved-Step", new { applicationNr, isApproved, isAutomatic })
                .EnsureSuccessStatusCode();
        }

        public RunFraudControlsResult RunFraudControls(string applicationNr)
        {
            return Begin()
                 .PostJson("api/FraudControl/RunFraudControls", new { applicationNr })
                 .ParseJsonAs<RunFraudControlsResult>();
        }

        public RunFraudControlsResult GetFraudControlItemApproved(string applicationNr)
        {
            return Begin()
                 .PostJson("api/FraudControl/GetFraudControls", new { applicationNr })
                 .ParseJsonAs<RunFraudControlsResult>();
        }

        public void SetFraudControlItemApproved(string fraudControlName, string applicationNr)
        {
            Begin()
                   .PostJson("api/FraudControl/SetFraudControlItemApproved", new { fraudControlName, applicationNr })
                   .EnsureSuccessStatusCode();
        }

        public void UnsecuredLoanStandardApproveFraudStep(string applicationNr, bool isApproved, bool isAutomatic)
        {
            Begin()
                .PostJson("api/UnsecuredLoanStandard/Fraud/Approve-Step", new { applicationNr, isApproved, isAutomatic })
                .EnsureSuccessStatusCode();
        }

        public void UnsecuredLoanStandardApproveAgreementStep(string applicationNr, bool isApproved, bool isAutomatic)
        {
            Begin()
                .PostJson("api/UnsecuredLoanStandard/Agreement/Set-Approved-Step", new { applicationNr, isApproved, isAutomatic })
                .EnsureSuccessStatusCode();
        }

        public void UnsecuredLoanStandardCreateAgreementSignatureSession(string applicationNr, string unsignedAgreementPdfArchiveKey, bool isAutomatic)
        {
            Begin()
                .PostJson("api/UnsecuredLoanStandard/Create-Agreement-Signature-Session", new { applicationNr, unsignedAgreementPdfArchiveKey, isAutomatic })
                .EnsureSuccessStatusCode();
        }
    }
}