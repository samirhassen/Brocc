using nPreCredit.Code.Services;
using nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation;
using NTech;
using NTech.ElectronicSignatures;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.ElectronicSignatures
{
    public class ElectronicSignatureProvider
    {
        private readonly IClock clock;

        public ElectronicSignatureProvider(IClock clock)
        {
            this.clock = clock;
        }

        public CommonElectronicIdSignatureSession CreateUnsecuredLoanStandardSignatureSession(ApplicationInfoModel ai, ApplicationInfoService applicationInfoService, string unsignedPdfArchiveKey)
        {
            var customerClient = new PreCreditCustomerClient();
            var applicants = applicationInfoService.GetApplicationApplicants(ai.ApplicationNr);

            var customerProperties = customerClient.BulkFetchPropertiesByCustomerIdsD(
                applicants.CustomerIdByApplicantNr.Values.ToHashSet(),
                "firstName", "lastName", "civicRegNr");

            var signers = Enumerable.Range(1, ai.NrOfApplicants).Select(applicantNr =>
            {
                var customerId = applicants.CustomerIdByApplicantNr[applicantNr];
                return new
                {
                    ApplicantNr = applicantNr,
                    CivicRegNr = customerProperties[customerId]["civicRegNr"],
                    FirstName = customerProperties[customerId].Opt("firstName"),
                    LastName = customerProperties[customerId].Opt("lastName")
                };
            }).ToList();

            var customerPagesApplicationUrl = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "portal/navigate-with-login",
                Tuple.Create("targetName", "Application"), Tuple.Create("targetCustomData", ai.ApplicationNr)).ToString();

            return CreateSingleDocumentSignatureSession(new SingleDocumentSignatureRequest
            {
                DocumentToSignArchiveKey = unsignedPdfArchiveKey,
                RedirectAfterFailedUrl = customerPagesApplicationUrl,
                RedirectAfterSuccessUrl = customerPagesApplicationUrl,
                ServerToServerCallbackUrl = Controllers.Api.ApiSignaturePostbackController.GetCallbackUrl().ToString(),
                CustomData = new Dictionary<string, string>
                {
                    { "ApplicationNr", ai.ApplicationNr },
                    { "SignatureSessionType", Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.UnsecuredLoanStandardAgreementSignatureV1.ToString() }
                },
                DocumentToSignFileName = "Agreement.pdf",
                SigningCustomers = signers.OrderBy(x => x.ApplicantNr).Select(x => new SingleDocumentSignatureRequest.SigningCustomer
                {
                    SignerNr = x.ApplicantNr,
                    CivicRegNr = x.CivicRegNr,
                    FirstName = x.FirstName,
                    LastName = x.LastName
                }).ToList()
            });
        }

        public CommonElectronicIdSignatureSession CreateSingleDocumentSignatureSession(SingleDocumentSignatureRequest request)
        {
            var client = new CommonSignatureClient();
            return client.CreateSession(request);
        }

        public CommonElectronicIdSignatureSession GetCommonSignatureSession(string sessionId, bool returnNullIfSessionDoesNotExist)
        {
            var client = new CommonSignatureClient();
            var result = client.GetSession(sessionId, false, false, allowSessionDoesNotExist: returnNullIfSessionDoesNotExist);
            if (returnNullIfSessionDoesNotExist && result.IsNoSuchSessionExists)
                return null;
            return result.Session;
        }

        public bool TryCloseSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            try
            {
                var client = new CommonSignatureClient();
                return client.GetSession(sessionId, true, false).WasClosed;
            }
            catch (Exception ex)
            {
                //Cancel is almost always done to deal with problems so we dont want cancel failing to prevent trying again. Better to just leave a hanging seession.
                NLog.Warning(ex, $"Exception trying to cancel signature session: {sessionId}");
                return false;
            }
        }

        public bool TryHandle(string sessionId, System.Web.Mvc.IDependencyResolver resolver)
        {
            var client = new CommonSignatureClient();
            var result = client.GetSession(sessionId, false, true);
            if (result.IsUnsupportedSessionType)
                return false;

            var session = result.Session;

            var sessionType = session?.GetCustomDataOpt("SignatureSessionType");

            if (sessionType == Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.UnsecuredLoanStandardAgreementSignatureV1.ToString())
            {
                var applicationNr = session.GetCustomDataOpt("ApplicationNr");
                if (applicationNr == null)
                    return true;

                var ai = resolver.Resolve<ApplicationInfoService>()
                    .GetApplicationInfo(applicationNr, true);

                if (ai == null)
                    return true;

                resolver.Resolve<UnsecuredLoanStandardAgreementService>().OnCommonSignatureEvent(ai, session);

                TryHandleAutomation(ai, resolver.Resolve<UnsecuredLoanStandardWorkflowService>(), resolver.Resolve<UnsecuredLoanStandardAgreementService>());

                return true;
            }
            else if (sessionType == Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.ManualSignature.ToString())
            {
                var now = clock.Now.DateTime;
                using (var context = new PreCreditContext())
                {
                    var manualSignatures = context.ManualSignatures.Where(f => f.SignatureSessionId == sessionId).ToList();

                    if (session.SignedPdf != null)
                    {
                        //NOTE: Kept this loop when refactoring. Can there actually be more than one here or is this code just poorly written?
                        manualSignatures.ForEach(a =>
                        {
                            a.SignedDate = now;
                            a.SignedDocumentArchiveKey = session.SignedPdf.ArchiveKey;
                        });
                        context.SaveChanges();
                    }
                }
                return true;
            }
            else if (sessionType == Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.DirectDebitConsentSignatureV1.ToString())
            {
                var applicationNr = session.GetCustomDataOpt("ApplicationNr");
                if (applicationNr == null)
                    return true;

                var applicationInfo = resolver.Resolve<ApplicationInfoService>()
                    .GetApplicationInfo(applicationNr, true);

                if (applicationInfo == null)
                    return true;

                resolver.Resolve<SwedishDirectDebitConsentDocumentService>()
                    .OnSignatureEvent(applicationInfo, session);
            }

            return false;
        }

        private void TryHandleAutomation(ApplicationInfoModel ai, UnsecuredLoanStandardWorkflowService wfService, UnsecuredLoanStandardAgreementService agreementService)
        {
            try
            {
                var automationHandler = new ApplicationAutomationHandler(ai, wfService, agreementService);
                if (automationHandler.ShouldAutoApproveAgreement())
                {
                    automationHandler.TryHandleCustomerSignsAgreementAutomation();
                }
            }
            catch (Exception ex)
            {
                NLog.Warning(ex, $"Exception trying to handle agreement step automation.");
            }
        }
    }
}