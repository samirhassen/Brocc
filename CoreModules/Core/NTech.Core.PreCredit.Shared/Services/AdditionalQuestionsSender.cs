using Newtonsoft.Json;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Services;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace nPreCredit.Code
{
    public class AdditionalQuestionsSender : IAdditionalQuestionsSender
    {
        private ICoreClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly ILoggingService loggingService;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly IPublishEventService eventService;
        private readonly ICustomerClient customerClient;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly IUserDisplayNameService userDisplayNameService;
        private readonly IShowInfoOnNextPageLoadService showInfoOnNextPageLoadService;

        public AdditionalQuestionsSender(INTechCurrentUserMetadata ntechCurrentUserMetadata,
            IUserDisplayNameService userDisplayNameService,
            IShowInfoOnNextPageLoadService showInfoOnNextPageLoadService,
            ICoreClock clock,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            IPreCreditContextFactoryService preCreditContextFactoryService,
            ILoggingService loggingService,
            INTechEmailServiceFactory emailServiceFactory,
            IPublishEventService eventService,
            ICustomerClient customerClient,
            IPreCreditEnvSettings envSettings)
        {
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.loggingService = loggingService;
            this.emailServiceFactory = emailServiceFactory;
            this.eventService = eventService;
            this.customerClient = customerClient;
            this.envSettings = envSettings;
            this.userDisplayNameService = userDisplayNameService;
            this.showInfoOnNextPageLoadService = showInfoOnNextPageLoadService;
        }

        public SendSendAdditionalQuestionsEmailResult SendSendAdditionalQuestionsEmail(string applicationNr) =>
            SendSendAdditionalQuestionsEmailExtended(applicationNr);

        public SendSendAdditionalQuestionsEmailResult SendSendAdditionalQuestionsEmailExtended(string applicationNr, Action<CreditApplicationOneTimeToken> observeToken = null)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var ah = context.CreditApplicationHeadersQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => new
                {
                    x.CreditCheckStatus,
                    x.IsActive,
                    x.CanSkipAdditionalQuestions,
                    x.CurrentCreditDecision
                }).Single();

                if (!ah.IsActive)
                {
                    return new SendSendAdditionalQuestionsEmailResult { success = false, failedMessage = "Application is not active" };
                }

                string msg;
                var tmp = AdditionalLoanSupport.HasAdditionalLoanOffer(applicationNr, context, out msg);
                if (!tmp.HasValue)
                    return new SendSendAdditionalQuestionsEmailResult { success = false, failedMessage = msg };

                var isAdditionalLoanOffer = tmp.Value;

                var appModel = partialCreditApplicationModelRepository.Get(applicationNr,
                                    applicantFields: new List<string>
                                    {
                                        "customerId",
                                    });
                var customerIdByApplicantNr = new Dictionary<int, int>();
                appModel.DoForEachApplicant(applicantNr =>
                {
                    var cid = appModel.Applicant(applicantNr).Get("customerId").IntValue.Optional;
                    if (cid.HasValue)
                        customerIdByApplicantNr[applicantNr] = cid.Value;
                });

                return SendSendAdditionalQuestionsEmailExtended(applicationNr, isAdditionalLoanOffer, appModel.NrOfApplicants, customerIdByApplicantNr, true, observeToken: observeToken);
            }
        }

        public SendSendAdditionalQuestionsEmailResult SendSendAdditionalQuestionsEmail(string applicationNr, bool isAdditionalLoanOffer, int nrOfApplicants, IDictionary<int, int> customerIdByApplicantNr, bool emitEvent) =>
            SendSendAdditionalQuestionsEmailExtended(applicationNr, isAdditionalLoanOffer, nrOfApplicants, customerIdByApplicantNr, emitEvent);

        private SendSendAdditionalQuestionsEmailResult SendSendAdditionalQuestionsEmailExtended(string applicationNr, bool isAdditionalLoanOffer, int nrOfApplicants, IDictionary<int, int> customerIdByApplicantNr, bool emitEvent,
            Action<CreditApplicationOneTimeToken> observeToken = null)
        {
            var now = this.clock.Now;
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var ah = context.CreditApplicationHeadersQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => new
                {
                    x.CreditCheckStatus,
                    x.IsActive,
                    x.CanSkipAdditionalQuestions,
                    x.CurrentCreditDecision
                }).Single();

                //Fetch main applicant email
                string failedMessage = "";
                Enumerable.Range(1, nrOfApplicants).ToList().ForEach(applicantNr =>
                {
                    var cid = customerIdByApplicantNr.ContainsKey(applicantNr) ? new int?(customerIdByApplicantNr[applicantNr]) : null;
                    if (!cid.HasValue)
                    {
                        failedMessage += " Missing customerId on applicant " + applicantNr;
                    }
                    else
                    {
                        var customerCardPropertyStatus = customerClient.CheckPropertyStatus(cid.Value, new HashSet<string>()
                        {
                            "email",
                            "firstName",
                            "civicRegNr"
                        });
                        if (customerCardPropertyStatus?.MissingPropertyNames?.Any() ?? false)
                        {
                            failedMessage += $" Applicant {applicantNr} has issues with the customer {customerCardPropertyStatus.GetMissingPropertyNamesIssueDescription()}";
                        }
                    }
                });

                if (failedMessage.Length > 0)
                    return new SendSendAdditionalQuestionsEmailResult { success = false, failedMessage = failedMessage };

                var mainApplicantCustomerId = customerIdByApplicantNr[1];
                var customerCardItems = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { mainApplicantCustomerId }, "email")[mainApplicantCustomerId];

                var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                if (!customerCardItems.ContainsKey("email") || !emailValidator.IsValid(customerCardItems["email"]))
                {
                    var m = customerCardItems.ContainsKey("email") ? "invalid email for main applicant" : "missing email for main applicant";
                    var userWarningMessage = $"Could not send additional questions: {m}";
                    var newComment = CreateComment(applicationNr, userWarningMessage, "AdditionalQuestionsFailed");
                    context.AddCreditApplicationComments(newComment);
                    context.SaveChanges();
                    return new SendSendAdditionalQuestionsEmailResult
                    {
                        success = false,
                        failedMessage = userWarningMessage,
                        newComment = new SendSendAdditionalQuestionsEmailResult.Comment
                        {
                            Id = newComment.Id,
                            CommentDate = newComment.CommentDate,
                            CommentText = newComment.CommentText,
                            CommentByName = userDisplayNameService.GetUserDisplayNameByUserId(newComment.CommentById.ToString())
                        }
                    };
                }

                Uri uri;
                CreditApplicationOneTimeToken token;
                context.BeginTransaction();
                try
                {
                    //Generate a unique token
                    token = UlLegacyAgreementSignatureService.CreateAdditionalQuestionsToken(applicationNr, ntechCurrentUserMetadata, now, isAdditionalLoanOffer, false);
                    observeToken?.Invoke(token);
                    context.AddCreditApplicationOneTimeTokens(token);
                    context.AddCreditApplicationComments(CreateComment(applicationNr, "Additional questions sent" + (isAdditionalLoanOffer ? " for additional loan" : ""), "AdditionalQuestionsSent"));

                    var email = customerCardItems["email"];

                    //Create the email link
                    uri = new Uri(envSettings.AdditionalQuestionsUrlPattern.Replace("{token}", token.Token));

                    var applicationWrapperUrlPattern = envSettings.ApplicationWrapperUrlPattern;
                    if (applicationWrapperUrlPattern != null)
                    {
                        //Send a wrapper link instead of the raw link so it can be switched to always point to the current state of the application
                        var wrapperToken = AgreementSigningProviderHelper.GetOrCreateApplicationWrapperToken(context, now, applicationNr, 1, ntechCurrentUserMetadata.UserId, ntechCurrentUserMetadata.InformationMetadata);
                        uri = new Uri(applicationWrapperUrlPattern.Replace("{token}", wrapperToken.Token));
                    }
                    context.SaveChanges();

                    var s = emailServiceFactory.CreateEmailService();

                    SendSendAdditionalQuestionsEmailResult CreateErrorResultAndLogError(Exception ex)
                    {
                        loggingService.Warning(ex, "Additional questions email error reported to user");
                        return new SendSendAdditionalQuestionsEmailResult
                        {
                            success = false,
                            failedMessage = "Email provider failed to send additional questions email. Make sure the email address is valid."
                        };
                    }

                    var resason = isAdditionalLoanOffer ? "AdditionalQuestionsAdditionalLoan" : "AdditionalQuestions";
                    try
                    {
                        s.SendTemplateEmail(
                            new List<string> { email },
                            isAdditionalLoanOffer ? "credit-additionalquestions-additionalloan" : "credit-additionalquestions",
                            new Dictionary<string, string> { { "link", uri.ToString() } },
                            $"Reason={resason}, ApplicationNr={applicationNr}");
                    }
                    catch (HttpRequestException ex)
                    {
                        return CreateErrorResultAndLogError(ex);
                    }
                    catch (NTechCoreWebserviceException ex)
                    {
                        if (ex.ErrorCode == "emailError")
                        {
                            return CreateErrorResultAndLogError(ex);
                        }
                        else
                            throw;
                    }

                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }


                if (envSettings.ShowDemoMessages || !envSettings.IsProduction)
                {
                    showInfoOnNextPageLoadService.ShowInfoMessageOnNextPageLoad("Additional questions link", uri.ToString(), uri);
                }

                if (emitEvent)
                    EmitAdditionalQuestionsSentEvent(applicationNr);

                return new SendSendAdditionalQuestionsEmailResult
                {
                    success = true,
                    isAdditionalLoanOffer = isAdditionalLoanOffer,
                    additionalQuestionsStatus = OneTimeTokenToAdditionlQuestionsStatus(token, ah.CanSkipAdditionalQuestions)
                };
            }
        }

        public void EmitAdditionalQuestionsSentEvent(string applicationNr)
        {
            eventService.Publish(PreCreditEventCode.CreditApplicationAdditionalQuestionsSent, JsonConvert.SerializeObject(new { applicationNr = applicationNr }));
        }

        private CreditApplicationComment CreateComment(string applicationNr, string commentText, string eventType)
        {
            var now = this.clock.Now;
            return new CreditApplicationComment
            {
                ApplicationNr = applicationNr,
                CommentText = CreditApplicationComment.CleanCommentText(commentText),
                CommentDate = now,
                ChangedDate = now,
                CommentById = ntechCurrentUserMetadata.UserId,
                ChangedById = ntechCurrentUserMetadata.UserId,
                EventType = eventType,
                InformationMetaData = ntechCurrentUserMetadata.InformationMetadata
            };
        }

        private SendSendAdditionalQuestionsEmailResult.QuestionStatus OneTimeTokenToAdditionlQuestionsStatus(CreditApplicationOneTimeToken token, bool canSkipAdditionalQuestions)
        {
            var extraData = token == null || token.TokenExtraData == null ? null : JsonConvert.DeserializeAnonymousType(token.TokenExtraData, new { hasAnswered = false });
            return new SendSendAdditionalQuestionsEmailResult.QuestionStatus
            {
                sentDate = token?.CreationDate,
                hasAnswered = extraData?.hasAnswered ?? false,
                canSkipAdditionalQuestions = canSkipAdditionalQuestions
            };
        }
    }

    public interface IAdditionalQuestionsSender
    {
        SendSendAdditionalQuestionsEmailResult SendSendAdditionalQuestionsEmail(string applicationNr, bool isAdditionalLoanOffer, int nrOfApplicants, IDictionary<int, int> customerIdByApplicantNr, bool emitEvent);
        SendSendAdditionalQuestionsEmailResult SendSendAdditionalQuestionsEmail(string applicationNr);
        void EmitAdditionalQuestionsSentEvent(string applicationNr);
    }

    public class SendSendAdditionalQuestionsEmailResult
    {
        public string failedMessage { get; set; }
        public bool success { get; set; }
        public Comment newComment { get; set; }
        public class Comment
        {
            public int Id { get; set; }
            public DateTimeOffset CommentDate { get; set; }
            public string CommentText { get; set; }
            public string CommentByName { get; set; }
        }
        public bool isAdditionalLoanOffer { get; set; }
        public QuestionStatus additionalQuestionsStatus { get; set; }

        public class QuestionStatus
        {
            public DateTimeOffset? sentDate { get; set; }
            public bool hasAnswered { get; set; }
            public bool canSkipAdditionalQuestions { get; set; }
        }
    }
}