using Newtonsoft.Json;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.Email;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageApplicationRejectionService : IMortgageApplicationRejectionService
    {
        private ApplicationInfoService applicationInfoService;
        private readonly IMortgageLoanApplicationCreditCheckService applicationCreditCheckService;
        private readonly IProviderInfoService providerInfoService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly IPublishEventService publishEventService;
        private readonly INTechEmailService emailService;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;

        public MortgageApplicationRejectionService(
            INTechCurrentUserMetadata ntechCurrentUserMetadata,
            IClock clock,
            INTechEmailService emailService,
            IPublishEventService publishEventService,
            ApplicationInfoService applicationInfoService,
            IMortgageLoanApplicationCreditCheckService applicationCreditCheckService,
            IProviderInfoService providerInfoService,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository)
        {
            this.applicationInfoService = applicationInfoService;
            this.applicationCreditCheckService = applicationCreditCheckService;
            this.providerInfoService = providerInfoService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.publishEventService = publishEventService;
            this.emailService = emailService;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
        }

        public bool TryReject(string applicationNr, bool? wasAutomated, bool skipProviderCallback, out string failedMessage, Action beforePublishEvent = null)
        {
            var automationCommentSuffix = (wasAutomated ?? false) ? " (automated)" : "";
            var ai = this.applicationInfoService.GetApplicationInfo(applicationNr);
            if (!ai.IsRejectAllowed)
            {
                failedMessage = "Reject is not allowed";
                return false;
            }
            List<string> rejectionReasons = new List<string>();
            string rejectionSummary = "";

            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var pre = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        App = x,
                        Decsion = x.CurrentCreditDecision
                    })
                    .Single();
                var a = pre.App;

                if (ai.MortgageLoanFinalCreditCheckStatus == "Rejected")
                {
                    var finalStatus = this.applicationCreditCheckService.FetchApplicationFinalStatus(ai.ApplicationNr);
                    rejectionReasons.AddRange(finalStatus.RejectedDecision.RejectionReasons.Select(x => x.Name));
                    rejectionSummary += (rejectionSummary.Length > 0 ? "," : "") + " Final credit check";
                }
                else if (ai.MortgageLoanInitialCreditCheckStatus == "Rejected")
                {
                    var initialStatus = this.applicationCreditCheckService.FetchApplicationInitialStatus(ai.ApplicationNr);
                    rejectionReasons.AddRange(initialStatus.RejectedDecision.RejectionReasons.Select(x => x.Name));
                    rejectionSummary += (rejectionSummary.Length > 0 ? "," : "") + " Initial offer";
                }

                if (ai.CustomerCheckStatus == "Rejected")
                {
                    rejectionReasons.Add("customerCheck");
                    rejectionSummary += (rejectionSummary.Length > 0 ? "," : "") + " Customer check";
                }

                if (ai.MortgageLoanDocumentCheckStatus == "Rejected")
                {
                    rejectionReasons.Add("documentCheck");
                    rejectionSummary += (rejectionSummary.Length > 0 ? "," : "") + " Document check";
                }

                if (rejectionReasons.Count == 0)
                {
                    failedMessage = "Cannot reject without a reason";
                    return false;
                }

                string commentEmailPart = "";
                var isSendingRejectionEmails = providerInfoService.GetSingle(a.ProviderName).IsSendingRejectionEmails;
                if (isSendingRejectionEmails)
                {
                    var appModel = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" });
                    var customerClient = new PreCreditCustomerClient();

                    var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                    var emailsPerApplicant = Enumerable
                        .Range(1, appModel.NrOfApplicants)
                        .Select(x =>
                        {
                            var customerId = appModel.Applicant(x).Get("customerId").IntValue.Required;
                            var kv = customerClient.GetCustomerCardItems(customerId, "email");

                            var value = kv.ContainsKey("email") ? kv["email"] : null;

                            var isMissing = string.IsNullOrWhiteSpace(value);
                            var isInvalid = !isMissing && !emailValidator.IsValid(value);

                            return new { applicantNr = x, email = value, isMissing = isMissing, isInvalid = isInvalid };
                        });

                    if (emailsPerApplicant.Any(x => x.isMissing || x.isInvalid))
                    {
                        var m = string.Join(", ", emailsPerApplicant.Select(x => x.isInvalid ? $"invalid email for applicant {x.applicantNr}" : $"missing email for applicant {x.applicantNr}"));
                        var userWarningMessage = $"Could not reject application since rejection emails could not be sent: {m}";

                        var newComment = context.CreateAndAddComment(userWarningMessage, "ApplicationRejectedFailed", applicationNr: applicationNr);

                        context.SaveChanges();

                        failedMessage = userWarningMessage;
                        return false;
                    }

                    SendRejectionEmails(emailsPerApplicant.Select(x => x.email).ToList(), rejectionReasons, applicationNr);

                    commentEmailPart = " and rejection email sent to applicants";
                }

                //Change to inactive
                var now = context.Clock.Now;
                a.IsActive = false;
                a.IsRejected = true;
                a.RejectedDate = now;
                a.RejectedById = context.CurrentUserId;

                var comment = context.CreateAndAddComment($"Application rejected due to {rejectionSummary} " + commentEmailPart + automationCommentSuffix, "ApplicationRejected" + ((wasAutomated ?? false) ? "Automated" : ""), applicationNr: applicationNr);

                context.SaveChanges();
            }

            beforePublishEvent?.Invoke();
            this.publishEventService.Publish(PreCreditEventCode.CreditApplicationRejected, JsonConvert.SerializeObject(new { applicationNr = applicationNr, wasAutomated = (wasAutomated ?? false), skipProviderCallback = skipProviderCallback }));

            failedMessage = null;
            return true;
        }

        private void SendRejectionEmails(List<string> emails, List<string> creditCheckRejectionReasons, string applicationNr)
        {
            var templateName = NEnv.ScoringSetup.GetRejectionEmailTemplateNameByRejectionReasons(creditCheckRejectionReasons);
            if (templateName != null)
            {
                var s = this.emailService;
                s.SendTemplateEmail(emails, templateName, null, $"Reason=CreditRejection, ApplicationNr={applicationNr}");
            }
            else
            {
                Log.Debug("Rejection email not sent for {ApplicationNr} since no template matches the current set of rejection reasons", applicationNr);
            }
        }
    }

    public interface IMortgageApplicationRejectionService
    {
        bool TryReject(string applicationNr, bool? wasAutomated, bool skipProviderCallback, out string failedMessage, Action beforePublishEvent = null);
    }
}