using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.ElectronicSignatures;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Services.Infrastructure.Email;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateUnsecuredLoanStandardAgreementSignatureSessionMethod : TypedWebserviceMethod<CreateUnsecuredLoanStandardAgreementSignatureSessionMethod.Request, CreateUnsecuredLoanStandardAgreementSignatureSessionMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Create-Agreement-Signature-Session";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var applicationInfoService = resolver.Resolve<ApplicationInfoService>();
            var ai = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);

            if (ai == null)
                return Error("Application does not exist");

            if (!ai.IsActive)
                return Error("Application is not active");

            var wf = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();
            var isStepCurrent =
                wf.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames);

            if (ai.HasLockedAgreement)
                return Error("Signature session already active");

            if (ai.IsFinalDecisionMade || !isStepCurrent)
                return Error("Application cannot be changed");

            var (currentCreditDecisionId, currentCreditDecisionLoanAmount) = GetCurrentCreditDecision(request, out var decisionExists);
            if (!decisionExists)
                return Error("Missing credit decision");

            var documentClient = new nDocumentClient();
            byte[] documentBytes;
            string unsignedAgreementPdfArchiveKey;
            if (request.DataUrlFile != null)
            {
                if (!FileUtilities.TryParseDataUrl(request.DataUrlFile.DataUrl, out var contentType, out var data))
                    return Error("Invalid DataUrl");
                if (!contentType.Contains("pdf"))
                    return Error("Document to sign must be a pdf");
                documentBytes = data;
                unsignedAgreementPdfArchiveKey = documentClient.ArchiveStore(data, contentType, request.DataUrlFile.FileName);
            }
            else
            {
                documentBytes = documentClient.FetchRaw(request.UnsignedAgreementPdfArchiveKey, out var contentType);
                if (!contentType.Contains("pdf"))
                    return Error("Document to sign must be a pdf");
                unsignedAgreementPdfArchiveKey = request.UnsignedAgreementPdfArchiveKey;
            }

            var signatureUrlByApplicantNr = new Dictionary<int, string>();

            var extraComment = "";

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var agreementSignatureSessionList = new Dictionary<string, string>();
                agreementSignatureSessionList["UnsignedAgreementPdfArchiveKey"] = unsignedAgreementPdfArchiveKey;
                agreementSignatureSessionList["IsSessionActive"] = "true";
                agreementSignatureSessionList["IsSessionFailed"] = "false";

                var signatureProvider = new ElectronicSignatureProvider(requestContext.Clock());
                var signatureSession = signatureProvider.CreateUnsecuredLoanStandardSignatureSession(ai, applicationInfoService, unsignedAgreementPdfArchiveKey);
                agreementSignatureSessionList["SignatureSessionId"] = signatureSession.Id;
                agreementSignatureSessionList["SignatureSessionProviderName"] = signatureSession.SignatureProviderName;

                //TODO: Refactor locked agreement to make it composable so we can have all this in a single transaction
                var lockedAgreementService = resolver.Resolve<ILockedAgreementService>();
                lockedAgreementService.LockAgreement(request.ApplicationNr, request.UnsignedAgreementPdfArchiveKey, currentCreditDecisionLoanAmount, currentCreditDecisionId);
                lockedAgreementService.TryApprovedLockedAgreement(request.ApplicationNr, false, out var _);

                ComplexApplicationListService.SetUniqueItems(
                    request.ApplicationNr, "AgreementSignatureSession", 1, agreementSignatureSessionList, context);

                string commentText = request.IsAutomatic ?? false ? "Signature session started automatically" : "Signature session started";
                context.CreateAndAddComment(commentText + extraComment, "SignatureSessionStarted", applicationNr: request.ApplicationNr);

                context.SaveChanges();

                // Send email here
                if (NTechEmailServiceFactory.HasEmailProvider)
                {
                    var mainApplicantCustomerId = int.Parse(context
                    .ComplexApplicationListItems
                    .Where(x => x.ApplicationNr == request.ApplicationNr && x.ListName == "Applicant" && x.Nr == 1 && x.ItemName == "customerId")
                    .Select(x => x.ItemValue)
                    .Single());

                    var mainApplicantEmail = new PreCreditCustomerClient().BulkFetchPropertiesByCustomerIdsSimple(
                                new HashSet<int> { mainApplicantCustomerId },
                                "email")
                                ?.Opt(mainApplicantCustomerId)
                                ?.Opt("email");

                    if (!string.IsNullOrWhiteSpace(mainApplicantEmail))
                    {
                        SendNotificationEmail(request.ApplicationNr, mainApplicantEmail,
                         () => context.CreateAndAddComment("Failed to send notification email. See error log for details.", "failedToSendNotificationEmail", applicationNr: request.ApplicationNr));
                    }
                    else
                    {
                        context.CreateAndAddComment("Main applicant email not found, could not send email notification. ", "failedToSendNotificationEmail", applicationNr: request.ApplicationNr);
                    }

                    context.SaveChanges();
                }

            }

            return new Response
            {
                SignatureUrlByApplicantNr = signatureUrlByApplicantNr
            };
        }

        private void SendNotificationEmail(string applicationNr, string mainApplicantEmail, Action addCommentOnfailToSend)
        {
            var emailTemplateService = DependancyInjection.Services.Resolve<LoanStandardEmailTemplateService>();
            var emailTemplate = emailTemplateService.LoadTemplate(
                "agreementReadyForSigningEmailTemplates",
                "genericSubjectTemplate",
                "genericBodyTemplate",
                "generic-application-update");

            if (!emailTemplate.IsEnabled)
                return;

            var customerClient = new PreCreditCustomerClient();
            var documentClientData = customerClient.LoadSettings("documentClientData");

            var customerPagesLink = GenerateCustomerPagesLink();
            var emailTemplateContext = new Dictionary<string, object>
            {
                { "clientDisplayName", documentClientData?.Opt("name") ?? "lender" },
                { "customerPagesLink", customerPagesLink }
            };
            var sendingContext = $"Reason=AgreementSignatureNotification, ApplicationNr={applicationNr}";

            var emailData = new LoanStandardEmailTemplateService.EmailTemplateData
            {
                RecipientEmails = new List<string> { mainApplicantEmail },
                SubjectTemplateText = emailTemplate.SubjectTemplateText,
                BodyTemplateText = emailTemplate.BodyTemplateText,
                EmailTemplateContext = emailTemplateContext,
                SendingContext = sendingContext,
                OnFailedToSend = addCommentOnfailToSend
            };

            emailTemplateService.SendEmail(emailData);
        }

        private string GenerateCustomerPagesLink()
        {
            var customerPagesTarget = "ApplicationsOverview";
            return NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "login/eid-signature", Tuple.Create("targetName", customerPagesTarget)).ToString();
        }

        private (int currentCreditDecisionId, decimal currentCreditDecisionLoanAmount) GetCurrentCreditDecision(Request request, out bool decisionExists)
        {
            int currentCreditDecisionId;
            decimal currentCreditDecisionLoanAmount;
            decisionExists = true;

            using (var context = new PreCreditContext())
            {
                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        CurrentCreditDecisionId = (int?)x.CurrentCreditDecision.Id,
                        CurrentCreditDecisionLoanAmount = x
                            .CurrentCreditDecision
                            .DecisionItems
                            .Where(y => y.ItemName == "loanAmount" && !y.IsRepeatable)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                    })
                    .Single();
                if (!application.CurrentCreditDecisionId.HasValue)
                    decisionExists = false;
                currentCreditDecisionId = application.CurrentCreditDecisionId.Value;
                currentCreditDecisionLoanAmount =
                    decimal.Parse(application.CurrentCreditDecisionLoanAmount, CultureInfo.InvariantCulture);
            }

            return (currentCreditDecisionId, currentCreditDecisionLoanAmount);
        }

        public class Request : IValidatableObject
        {
            [Required]
            public string ApplicationNr { get; set; }

            public string UnsignedAgreementPdfArchiveKey { get; set; }

            public DataUrlFileModel DataUrlFile { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var request = (Request)validationContext.ObjectInstance;
                if ((request?.DataUrlFile == null) == string.IsNullOrWhiteSpace(request.UnsignedAgreementPdfArchiveKey))
                {
                    yield return new ValidationResult("Exactly one of UnsignedAgreementPdfArchiveKey and DataUrlFile required");
                }
            }

            public class DataUrlFileModel
            {
                [Required]
                public string FileName { get; set; }

                [Required]
                public string DataUrl { get; set; }
            }

            public bool? IsAutomatic { get; set; }
        }

        public class Response
        {
            //TODO: Returned to make testing easier. May think about if this makes sense here going forward.
            public Dictionary<int, string> SignatureUrlByApplicantNr { get; set; }
        }
    }
}