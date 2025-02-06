using Newtonsoft.Json;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.LegacyUnsecuredLoans
{
    public class LegacyUnsecuredLoansRejectionService
    {
        public LegacyUnsecuredLoansRejectionService(IApplicationCommentServiceComposable applicationCommentService, ICoreClock clock,
            IPreCreditContextFactoryService preCreditContextFactoryService, ILoggingService loggingService, IPreCreditEnvSettings envSettings,
            INTechEmailService emailService, IPublishEventService eventService,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, ICustomerClient customerClient)
        {
            this.applicationCommentService = applicationCommentService;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.loggingService = loggingService;
            this.envSettings = envSettings;
            this.emailService = emailService;
            this.eventService = eventService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerClient = customerClient;
            this.clock = clock;
        }

        public (bool IsSuccess, string UserWarningMessage) TryRejectApplication(string applicationNr, bool wasAutomated)
        {
            try
            {
                var result = RejectApplication(applicationNr, wasAutomated);
                var userWarningMessage = result.WasRejectionEmailFailed ? result.RejectionEmailFailedMessage : null;
                return (IsSuccess: true, UserWarningMessage: userWarningMessage);
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.IsUserFacing)
                {
                    return (IsSuccess: false, UserWarningMessage: ex.Message);
                }
                else
                {
                    loggingService.Error(ex, "Auto reject application after rejected credit check failed for unknown reason");
                    return (IsSuccess: false, UserWarningMessage: null);
                }
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, "Auto reject application after rejected credit check failed for unknown reason");
                return (IsSuccess: false, UserWarningMessage: null);
            }
        }

        public (bool WasRejectionEmailFailed, string RejectionEmailFailedMessage) RejectApplication(string applicationNr, bool? wasAutomated)
        {
            var automationCommentSuffix = (wasAutomated ?? false) ? " (automated)" : "";

            string providerName = null;
            const string rejectionEmailFailedMessage = "Application rejected but email could not be sent";
            bool wasRejectionEmailFailed = false;
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var pre = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        App = x,
                        Decsion = x.CurrentCreditDecision,
                        DocumentCheckStatus = (x.Items.Where(y => y.Name == "documentCheckStatus").Select(y => y.Value).FirstOrDefault() ?? "Initial")
                    })
                    .Single();
                var a = pre.App;
                var documentCheckStatus = pre.DocumentCheckStatus;
                providerName = pre.App.ProviderName;

                //Figure out rejection reasons
                var rejection = pre.Decsion as RejectedCreditDecision;

                List<string> rejectionReasons = new List<string>();
                if (rejection != null)
                {
                    var rj = CreditDecisionModelParser.ParseRejectionReasons(rejection.RejectedDecisionModel);
                    if (rj != null && rj.Length > 0)
                        rejectionReasons.AddRange(rj);
                }
                if (a.CustomerCheckStatus == "Rejected")
                {
                    rejectionReasons.Add("customerCheck");
                }
                if (documentCheckStatus == "Rejected")
                {
                    rejectionReasons.Add("documentCheck");
                }

                var fraudControl = context.FraudControlsQueryable.Where(x => x.ApplicationNr == applicationNr && x.IsCurrentData).SingleOrDefault();
                if (fraudControl != null && fraudControl.RejectionReasons != null)
                {
                    List<string> fraudRejectionReasons = JsonConvert.DeserializeAnonymousType(fraudControl.RejectionReasons, new List<string>());
                    rejectionReasons.Add("fraudCheck");
                }

                if (rejectionReasons.Count == 0)
                    throw new NTechCoreWebserviceException("Cannot reject without a reason")
                    {
                        ErrorCode = "rejectionReasonMissing",
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };

                string commentEmailPart = "";
                var isSendingRejectionEmails = envSettings.GetAffiliateModel(a.ProviderName).IsSendingRejectionEmails;
                if (isSendingRejectionEmails)
                {
                    var appModel = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" });

                    var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                    var emailsPerApplicant = Enumerable
                        .Range(1, appModel.NrOfApplicants)
                        .Select(x =>
                        {
                            var customerId = appModel.Applicant(x).Get("customerId").IntValue.Required;
                            var kv = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "email")[customerId];

                            var value = kv.ContainsKey("email") ? kv["email"] : null;

                            var isMissing = string.IsNullOrWhiteSpace(value);
                            var isInvalid = !isMissing && !emailValidator.IsValid(value);

                            return new { applicantNr = x, email = value, isMissing = isMissing, isInvalid = isInvalid };
                        });

                    if (emailsPerApplicant.Any(x => x.isMissing || x.isInvalid))
                    {
                        var m = string.Join(", ", emailsPerApplicant.Select(x => x.isInvalid ? $"invalid email for applicant {x.applicantNr}" : $"missing email for applicant {x.applicantNr}"));
                        var userWarningMessage = $"Could not reject application since rejection emails could not be sent: {m}";

                        applicationCommentService.TryAddCommentComposable(applicationNr, userWarningMessage, "ApplicationRejectedFailed", null, out var _, context);

                        context.SaveChanges();

                        throw new NTechCoreWebserviceException(userWarningMessage)
                        {
                            ErrorCode = "invalidOrMissingApplicantEmail",
                            ErrorHttpStatusCode = 400,
                            IsUserFacing = true
                        };
                    }

                    if (!TrySendRejectionEmails(emailsPerApplicant.Select(x => x.email).ToList(), rejectionReasons, applicationNr, out var failedToSendEmailMessage))
                    {
                        var isSkipRejectionEmailAllowed = !rejectionReasons.Except(rejectionReasonSkippableRejectionEmailWhitelist).Any();
                        if (!isSkipRejectionEmailAllowed)
                        {
                            var rejectAbortedMessage = "Reject application aborted since email could not be sent.";

                            applicationCommentService.TryAddCommentComposable(applicationNr, rejectAbortedMessage, "ApplicationRejectedFailedEmail", null, out var _, context);

                            context.SaveChanges();

                            throw new NTechCoreWebserviceException(rejectAbortedMessage)
                            {
                                ErrorCode = "failedToSendRejectionEmail",
                                ErrorHttpStatusCode = 400,
                                IsUserFacing = true
                            };
                        }
                        else
                        {
                            wasRejectionEmailFailed = true;
                        }
                    }

                    if (!wasRejectionEmailFailed)
                    {
                        commentEmailPart = " and rejection email sent to applicants";
                    }
                }

                //Change to inactive
                var now = clock.Now;
                a.IsActive = false;
                a.IsRejected = true;
                a.RejectedDate = now;
                a.RejectedById = context.CurrentUserId;

                applicationCommentService.TryAddCommentComposable(applicationNr,
                    "Application rejected" + commentEmailPart + automationCommentSuffix, "ApplicationRejected" + ((wasAutomated ?? false) ? "Automated" : ""),
                    null, out var _, context);

                if (wasRejectionEmailFailed)
                {
                    applicationCommentService.TryAddCommentComposable(
                        applicationNr,
                        rejectionEmailFailedMessage,
                        "ApplicationRejectedEmailNotSent", null, out var _, context);
                }

                context.SaveChanges();
            }

            eventService.Publish(PreCreditEventCode.CreditApplicationRejected, JsonConvert.SerializeObject(new
            {
                applicationNr = applicationNr,
                wasAutomated = (wasAutomated ?? false),
                providerName = providerName
            }));

            return (WasRejectionEmailFailed: wasRejectionEmailFailed, RejectionEmailFailedMessage: rejectionEmailFailedMessage);
        }

        //If all the rejection reasons are in this list it's ok to proceed with rejecting the application even if the rejection email cannot be sent.
        private static HashSet<string> rejectionReasonSkippableRejectionEmailWhitelist = new HashSet<string>
        {
            "documentCheck", "customerCheck", "fraudCheck"
        };
        private readonly IApplicationCommentServiceComposable applicationCommentService;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly ILoggingService loggingService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly INTechEmailService emailService;
        private readonly IPublishEventService eventService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly ICustomerClient customerClient;
        private readonly ICoreClock clock;

        private bool TrySendRejectionEmails(List<string> emails, List<string> creditCheckRejectionReasons, string applicationNr, out string failureReasonMessage)
        {
            var templateName = envSettings.ScoringSetup.GetRejectionEmailTemplateNameByRejectionReasons(creditCheckRejectionReasons);
            if (templateName == null)
            {
                failureReasonMessage = "Missing email template";
                return false;
            }


            try
            {
                emailService.SendTemplateEmail(emails, templateName, null, $"Reason=CreditRejection, ApplicationNr={applicationNr}");
                failureReasonMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                failureReasonMessage = "Email could not be sent";
                return false;
            }
        }
    }
}