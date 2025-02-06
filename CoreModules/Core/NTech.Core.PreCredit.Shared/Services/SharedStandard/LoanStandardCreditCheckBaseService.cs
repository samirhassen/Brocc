using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.SharedStandard
{
    public abstract class LoanStandardCreditCheckBaseService
    {
        private readonly IApplicationCommentService applicationCommentService;
        protected readonly IPreCreditEnvSettings envSettings;
        protected readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly ICustomerClient customerClient;
        private readonly IMarkdownTemplateRenderingService templateRenderingService;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly LoanStandardEmailTemplateService emailTemplateService;
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly bool isMortgageLoan;

        public LoanStandardCreditCheckBaseService(IApplicationCommentService applicationCommentService, IPreCreditEnvSettings envSettings, IPreCreditContextFactoryService preCreditContextFactory,
            ICustomerClient customerClient, IMarkdownTemplateRenderingService templateRenderingService, INTechEmailServiceFactory emailServiceFactory, LoanStandardEmailTemplateService emailTemplateService,
            INTechServiceRegistry serviceRegistry)
        {
            this.applicationCommentService = applicationCommentService;
            this.envSettings = envSettings;
            this.preCreditContextFactory = preCreditContextFactory;
            this.customerClient = customerClient;
            this.templateRenderingService = templateRenderingService;
            this.emailServiceFactory = emailServiceFactory;
            this.emailTemplateService = emailTemplateService;
            this.serviceRegistry = serviceRegistry;
            this.isMortgageLoan = envSettings.IsMortgageLoansEnabled || envSettings.IsStandardMortgageLoansEnabled;
        }

        protected DecisionNotificationData GetDecisionNotificationDataIfPossible(HashSet<string> rejectionReasons, bool supressUserNotification, string applicationNr, bool isFinalCreditCheck, out string failedCommentPart)
        {
            failedCommentPart = null;

            var isOffer = rejectionReasons == null;

            if (supressUserNotification)
            {
                failedCommentPart = " (notification supressed)";
                return null;
            }

            using (var context = preCreditContextFactory.CreateExtended())
            {
                var mainApplicantCustomerId = int.Parse(context
                    .ComplexApplicationListItemsQueryable
                    .Where(x => x.ApplicationNr == applicationNr && x.ListName == "Applicant" && x.Nr == 1 && x.ItemName == "customerId")
                    .Select(x => x.ItemValue)
                    .Single());

                if (!isOffer)
                {
                    //Rejected decision, send secure message with details
                    var templateSetting = customerClient.LoadSettings("creditRejectionSecureMessageTemplates");

                    if (templateSetting.Opt("isEnabled") != "true")
                        return null;

                    var templateName = rejectionReasons.Contains("paymentRemark")
                        ? "paymentRemarkTemplate"
                        : "generalTemplate";

                    var templateText = templateSetting.Opt(templateName);
                    if (string.IsNullOrWhiteSpace(templateText))
                    {
                        failedCommentPart = $" (notification not sent since the template {templateName} is missing)";
                        return null;
                    }

                    var htmlText = templateRenderingService.RenderTemplateToHtml(templateSetting[templateName]);

                    return new DecisionNotificationData
                    {
                        IsSecureMessage = true,
                        SecureMessageData = new DecisionNotificationData.SecureMessageDataModel
                        {
                            CustomerId = mainApplicantCustomerId,
                            HtmlText = htmlText,
                            ApplicationNr = applicationNr
                        }
                    };
                }
                else
                {
                    //Offer / Accepted decision, send notification to login and continue the process
                    if (!emailServiceFactory.HasEmailProvider)
                    {
                        //We likely dont want this notified on every single application. 
                        return null;
                    }

                    (string SubjectTemplateText, string BodyTemplateText, bool IsEnabled) emailTemplate;
                    if (isMortgageLoan)
                    {
                        emailTemplate = emailTemplateService.LoadTemplate(
                            isFinalCreditCheck ? "finalCreditCheckApproveEmailTemplates" : "initialCreditCheckApproveEmailTemplates",
                            "genericSubjectTemplate",
                            "genericBodyTemplate",
                            "generic-application-update");
                    }
                    else
                    {
                        emailTemplate = emailTemplateService.LoadTemplate(
                            "creditCheckApproveEmailTemplates",
                            "genericSubjectTemplate",
                            "genericBodyTemplate",
                            "generic-application-update");
                    }

                    if (!emailTemplate.IsEnabled)
                        return null;

                    var mainApplicantEmail = customerClient.BulkFetchPropertiesByCustomerIdsD(
                        new HashSet<int> { mainApplicantCustomerId },
                        "email")
                        ?.Opt(mainApplicantCustomerId)
                        ?.Opt("email");

                    if (string.IsNullOrWhiteSpace(mainApplicantEmail))
                    {
                        failedCommentPart = " (no notification email will be sent since no main applicant email was found)";
                        return null;
                    }

                    var documentClientData = customerClient.LoadSettings("documentClientData");

                    var customerPagesTarget = "ApplicationsOverview";
                    var customerPagesLink = serviceRegistry.ExternalServiceUrl("nCustomerPages", "login/eid-signature", Tuple.Create("targetName", customerPagesTarget)).ToString();

                    var emailTemplateContext = new Dictionary<string, object>
                    {
                        { "clientDisplayName", documentClientData?.Opt("name") ?? "lender" },
                        { "customerPagesLink", customerPagesLink }
                    };

                    var sendingContext = $"Reason={(rejectionReasons == null ? "OfferNotification" : "RejectionNotificaiton")}, ApplicationNr={applicationNr}";

                    return new DecisionNotificationData
                    {
                        IsSecureMessage = false,
                        EmailData = new LoanStandardEmailTemplateService.EmailTemplateData
                        {
                            RecipientEmails = new List<string> { mainApplicantEmail },
                            SubjectTemplateText = emailTemplate.SubjectTemplateText,
                            BodyTemplateText = emailTemplate.BodyTemplateText,
                            EmailTemplateContext = emailTemplateContext,
                            SendingContext = sendingContext,
                            OnFailedToSend = () => applicationCommentService.TryAddComment(applicationNr, "Failed to send notification email. See error log for details.", "failedToSendNotificationEmail", null, out var _)
                        }
                    };
                }
            }
        }
        protected class DecisionNotificationData
        {
            public bool IsSecureMessage { get; set; }
            public LoanStandardEmailTemplateService.EmailTemplateData EmailData { get; set; }
            public SecureMessageDataModel SecureMessageData { get; set; }
            public class SecureMessageDataModel
            {
                public int CustomerId { get; set; }
                public string HtmlText { get; set; }
                public string ApplicationNr { get; set; }
            }
        }

        protected void SendDecisionNotification(DecisionNotificationData notification)
        {
            if (notification.IsSecureMessage)
            {
                var d = notification.SecureMessageData;
                customerClient.SendHtmlSecureMessageWithEmailNotification(d.CustomerId, d.ApplicationNr,
                    isMortgageLoan ? "Application_MortgageLoan" : "Application_UnsecuredLoan", d.HtmlText);
            }
            else
            {
                emailTemplateService.SendEmail(notification.EmailData);
            }
        }
    }
}