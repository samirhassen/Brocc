using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.CustomerPages
{
    public class CustomerConfirmDirectDebitAccountInfoMethod :
        TypedWebserviceMethod<CustomerConfirmDirectDebitAccountInfoMethod.Request, CustomerConfirmDirectDebitAccountInfoMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerPages/Confirm-DirectDebit-Account";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var applicationInfoService = requestContext
                .Resolver()
                .Resolve<ApplicationInfoService>();

            var applicationInfo = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);

            if (applicationInfo == null)
                return Error("Not found");

            if (!applicationInfo.IsActive)
                return Error("Application is not active");

            var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);
            if (!applicants.CustomerIdByApplicantNr.Values.Contains(request.CustomerId.Value))
                return Error("No such application exists");

            var consentDocumentService = requestContext.Resolver().Resolve<SwedishDirectDebitConsentDocumentService>();
            var complexApplicationListService = requestContext.Resolver().Resolve<IComplexApplicationListReadOnlyService>();

            var documentClient = new nDocumentClient();

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var lists = complexApplicationListService.GetListsForApplication(request.ApplicationNr, true, context, "Application", "DirectDebitSigningSession");
                var directDebitRow = lists["DirectDebitSigningSession"].GetRow(1, true);

                var hasActiveSession = directDebitRow.GetUniqueItemBoolean("IsSessionActive", false);
                if (hasActiveSession != null)
                {
                    return Error("There is already an active direct debit consent file signing session. ");
                }

                var applicationRow = lists["Application"].GetRow(1, true);
                var directDebitAccountOwnerApplicantNr = applicationRow.GetUniqueItemInteger("directDebitAccountOwnerApplicantNr", false);

                if (directDebitAccountOwnerApplicantNr == null)
                {
                    return Error("No direct debit account owner is defined. ");
                }

                if (!consentDocumentService.TryCreateUnsignedDirectDebitConsentPdfForApplication(applicationInfo, out var consentFileArchiveKey, out var failedCode))
                {
                    return Error(failedCode, httpStatusCode: 400, errorCode: failedCode);
                }

                var consentFileSessionList = new Dictionary<string, string>();
                consentFileSessionList["UnsignedDirectDebitConsentFilePdfArchiveKey"] = consentFileArchiveKey;
                consentFileSessionList["IsSessionActive"] = "true";
                consentFileSessionList["IsSessionFailed"] = "false";

                var customerClient = new PreCreditCustomerClient();
                var signatureClient = new CommonSignatureClient();
                var accountOwnerCustomerId = applicants.CustomerIdByApplicantNr[directDebitAccountOwnerApplicantNr.Value];

                var customerProperties = customerClient.BulkFetchPropertiesByCustomerIdsSimple(
                    applicants.CustomerIdByApplicantNr.Values.ToHashSet(),
                    "firstName", "lastName", "civicRegNr");

                var signingCustomersByApplicantNr = new List<SingleDocumentSignatureRequest.SigningCustomer>()
                {
                    new SingleDocumentSignatureRequest.SigningCustomer
                    {
                        SignerNr = 1,
                        CivicRegNr = customerProperties[accountOwnerCustomerId]["civicRegNr"],
                        FirstName = customerProperties[accountOwnerCustomerId].Opt("firstName"),
                        LastName = customerProperties[accountOwnerCustomerId].Opt("lastName")
                    }
                };

                var customerPagesApplicationUrl = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages",
                    "portal/navigate-with-login",
                    Tuple.Create("targetName", "Application"),
                    Tuple.Create("targetCustomData", request.ApplicationNr)).ToString();

                var session = signatureClient.CreateSession(new SingleDocumentSignatureRequest
                {
                    DocumentToSignArchiveKey = consentFileArchiveKey,
                    DocumentToSignFileName = "DirectDebitConsent.pdf",
                    ServerToServerCallbackUrl = Controllers.Api.ApiSignaturePostbackController.GetCallbackUrl().ToString(),
                    SigningCustomers = signingCustomersByApplicantNr,
                    CustomData = new Dictionary<string, string>
                            {
                                { "ApplicationNr", request.ApplicationNr },
                                { "ApplicantNr", directDebitAccountOwnerApplicantNr.Value.ToString() },
                                {
                                    "SignatureSessionType",
                                    Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.DirectDebitConsentSignatureV1.ToString()
                                }
                            },
                    RedirectAfterFailedUrl = customerPagesApplicationUrl,
                    RedirectAfterSuccessUrl = customerPagesApplicationUrl
                });

                consentFileSessionList["SigningSessionProviderName"] = session.SignatureProviderName;
                consentFileSessionList["SigningSessionid"] = session.Id;

                ComplexApplicationListService.SetUniqueItems(request.ApplicationNr,
                    "DirectDebitSigningSession", 1, consentFileSessionList, context);

                context.SaveChanges();
            }

            return new Response { };
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response { }
    }


}