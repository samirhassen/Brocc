using Dapper;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Clients;
using NTech.Core;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.LegacyUnsecuredLoans
{
    public class UlLegacyAdditionalQuestionsService
    {
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly ICustomerClient customerClient;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ISignicatSigningClientReadOnly signicatSigningClient;
        private readonly CreditManagementWorkListService creditManagementWorkListService;
        private readonly UlLegacyAgreementSignatureService agreementSignatureService;
        private readonly ICoreClock clock;
        private readonly ApplicationInfoService applicationInfoService;
        private readonly ILoggingService loggingService;
        private readonly INTechServiceRegistry serviceRegistry;

        public UlLegacyAdditionalQuestionsService(IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            IPreCreditContextFactoryService preCreditContextFactory, ICustomerClient customerClient, IPreCreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration, ISignicatSigningClientReadOnly signicatSigningClient,
            CreditManagementWorkListService creditManagementWorkListService, UlLegacyAgreementSignatureService agreementSignatureService,
            ICoreClock clock, ApplicationInfoService applicationInfoService, ILoggingService loggingService, INTechServiceRegistry serviceRegistry)
        {
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.preCreditContextFactory = preCreditContextFactory;
            this.customerClient = customerClient;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
            this.signicatSigningClient = signicatSigningClient;
            this.creditManagementWorkListService = creditManagementWorkListService;
            this.agreementSignatureService = agreementSignatureService;
            this.clock = clock;
            this.applicationInfoService = applicationInfoService;
            this.loggingService = loggingService;
            this.serviceRegistry = serviceRegistry;
        }

        public bool IsKycErrorHandlingSupressed { get; set; } = false;

        public IQueryable<CreditApplicationHeader> ApplicationsByTokenQuery(IPreCreditContextExtended context, string token)
        {
            return context
                .CreditApplicationHeadersQueryable
                .Where(x => x.OneTimeTokens.Any(y => y.Token == token && (y.TokenType == "AdditionalQuestions" || y.TokenType.StartsWith("ApplicationWrapperToken"))));
        }

        public class ApplicationState
        {
            public int NrOfApplicants { get; set; }
            public bool IsActive { get; set; }
            public string Token { get; set; }
            public ActiveStateModel ActiveState { get; set; }
            public ClosedStateModel ClosedState { get; set; }
            public string ApplicationNr { get; internal set; }
            public bool IsForcedBankAccountDataSharing { get; set; }

            public class ClosedStateModel
            {
                public bool WasAccepted { get; set; }
            }

            public class ActiveStateModel
            {
                public bool IsWaitingForClient { get; set; }
                public bool ShouldAnswerAdditionalQuestions { get; set; }
                public AdditionalQuestionsInitialDataModel AdditionalQuestionsData { get; set; }
                public bool ShouldAnswerExternalAdditionalQuestions { get; set; }
                public ExternalAdditionalQuestionsInitialDataModel ExternalAdditionalQuestionsData { get; set; }
                public bool ShouldSignAgreements { get; set; }
                public AgreementInitialModel AgreementsData { get; set; }
                public DocumentSourceDataModel DocumentSourceData { get; set; }
                public bool IsAwaitingFinalApproval { get; set; }

                public bool IsWatingForDocumentUpload { get; set; }
                public bool IsWaitingForSharedAccountDataCallback { get; set; }
                public bool ShouldChooseDocumentSource { get; set; }
                public DocumentUploadDataModel DocumentUploadData { get; set; }
                public class DocumentUploadDataModel
                {
                    public Applicant Applicant1 { get; set; }
                    public Applicant Applicant2 { get; set; }

                    public class Applicant : ApplicantInitialModel
                    {
                        public string SharedAccountDataPdfPreviewArchiveKey { get; set; }
                    }
                }

                public class DocumentSourceDataModel
                {
                    public bool HasApplicant1ChosenDataSource { get; set; }
                    public bool HasApplicant2ChosenDataSource { get; set; }
                    public ApplicantInitialModel Applicant1 { get; set; }
                    public ApplicantInitialModel Applicant2 { get; set; }
                }

                public class ApplicantInitialModel
                {
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    public string CivicRegNr { get; set; }
                    public bool IsMissingAgreementProperties { get; set; }
                }

                public class AdditionalQuestionsInitialDataModel
                {
                    public bool IsAdditionalLoanOffer { get; set; }
                    public ApplicantInitialModel Applicant1 { get; set; }
                    public ApplicantInitialModel Applicant2 { get; set; }
                }

                public class ExternalAdditionalQuestionsInitialDataModel
                {
                    public string RedirectUrl { get; set; }
                }

                public class AgreementInitialModel
                {
                    public bool HasApplicant1SignedAgreement { get; set; }
                    public bool HasApplicant2SignedAgreement { get; set; }
                    public ApplicantInitialModel Applicant1 { get; set; }
                    public ApplicantInitialModel Applicant2 { get; set; }
                }
            }
        }

        public ApplicationState GetApplicationState(string token)
        {
            var result = GetApplicationStateI(token);
            UlLegacyAgreementSignatureService.UpdateCustomerCheckStatus(result.ApplicationNr, partialCreditApplicationModelRepository,
                preCreditContextFactory, customerClient);

            return GetApplicationStateI(token);
        }

        private ISet<int> GetCustomersThatSignedAgreement(string applicationNr, int nrOfApplicants, List<Tuple<string, string>> signedAgreementKeyItems)
        {
            var nrs = new HashSet<int>();
            foreach (var applicantNr in Enumerable.Range(1, nrOfApplicants))
            {
                if (signedAgreementKeyItems.Any(x => x.Item1 == $"document{applicantNr}"))
                {
                    nrs.Add(applicantNr);
                }
            }
            if (nrs.Count < nrOfApplicants && nrOfApplicants > 1 && envSettings.SignatureProvider == SignatureProviderCode.signicat)
            {
                //Since all the applicants sign the same document here we cant rely on the key being present to know if the first user signed or not since it wont be available until both have signed
                var session = signicatSigningClient.GetSignatureSessionByAlternateKey(AgreementSigningProviderShared.GetAlternateSignicatKey(applicationNr), false);
                if (session != null && session.HasSigned(1))
                    nrs.Add(1);
            }
            return nrs;
        }

        private ApplicationState GetApplicationStateI(string token)
        {
            using (var c = preCreditContextFactory.CreateExtended())
            {
                //Active
                var listStates = creditManagementWorkListService.GetSearchModel(c, true);

                var app = ApplicationsByTokenQuery(c, token)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.NrOfApplicants,
                        x.CreditCheckStatus, //For sanity check purposes
                        x.CustomerCheckStatus,
                        x.AgreementStatus,
                        x.IsActive,
                        x.IsFinalDecisionMade,
                        x.IsPartiallyApproved,
                        CustomerIdItems = x
                            .Items
                            .Where(y => y.Name == "customerId")
                            .Select(y => new
                            {
                                y.GroupName,
                                y.Value
                            }),
                        SignedAgreementKeyItems = x
                            .Items
                            .Where(y => y.Name == AgreementSigningProviderShared.SignedDocumentItemName)
                            .Select(y => new
                            {
                                y.GroupName,
                                y.Value
                            }),
                        DocumentCheckStatus = (x.Items.Where(y => y.Name == "documentCheckStatus").Select(y => y.Value).FirstOrDefault() ?? "Initial"),
                        DocumentSourceStatusApplicant1 = (x.Items.Where(y => y.GroupName == "applicant1" && y.Name == "documentSourceStatus").Select(y => y.Value).FirstOrDefault() ?? "pendingSelection"),
                        DocumentSourceStatusApplicant2 = (x.Items.Where(y => y.GroupName == "applicant2" && y.Name == "documentSourceStatus").Select(y => y.Value).FirstOrDefault() ?? "pendingSelection"),
                        SharedAccountDataPdfArchiveKey1 = x.Items.Where(y => y.GroupName == "applicant1" && y.Name == BankAccountDataShareServiceBase.PdfArchiveKeyApplicantItemName).Select(y => y.Value).FirstOrDefault(),
                        SharedAccountDataPdfArchiveKey2 = x.Items.Where(y => y.GroupName == "applicant2" && y.Name == BankAccountDataShareServiceBase.PdfArchiveKeyApplicantItemName).Select(y => y.Value).FirstOrDefault(),
                        HasAttachedDocuments = x.Documents.Any(y => !y.RemovedByUserId.HasValue && y.DocumentType == CreditApplicationDocumentTypeCode.DocumentCheck.ToString()),
                        State = listStates.Where(y => y.ApplicationNr == x.ApplicationNr).FirstOrDefault(),
                        x.CurrentCreditDecision,
                        IsForcedBankAccountDataSharing = x.Items.Where(y => y.GroupName == "application" && y.Name == "IsForcedBankAccountDataSharing" && y.Value.ToLower() == "true").Any(),
                        UserLanguage = x.Items.Where(y => y.GroupName == "application" && y.Name == "userLanguage").Select(y => y.Value).FirstOrDefault()
                    })
                    .SingleOrDefault();

                if (app == null)
                {
                    return null;
                }

                Lazy<Dictionary<int, int>> customerIdByApplicantNr = new Lazy<Dictionary<int, int>>(() => Enumerable
                    .Range(1, app.NrOfApplicants)
                    .ToDictionary(applicantNr => applicantNr, applicantNr => int.Parse(app.CustomerIdItems.Single(x => x.GroupName == $"applicant{applicantNr}").Value)));

                Action<Action<int, ApplicationState.ActiveStateModel.ApplicantInitialModel>> loadApplicants = handle =>
                {
                    foreach (var applicantNr in customerIdByApplicantNr.Value.Keys)
                    {
                        var customerId = customerIdByApplicantNr.Value[applicantNr];
                        var customerItems = customerClient.BulkFetchPropertiesByCustomerIdsD(
                            new HashSet<int> { customerId },
                            "civicRegNr", "firstName", "lastName", "addressZipcode", "email", "phone").Opt(customerId);
                        var a = new ApplicationState.ActiveStateModel.ApplicantInitialModel
                        {
                            CivicRegNr = customerItems["civicRegNr"],
                            FirstName = customerItems.Opt("firstName"),
                            LastName = customerItems.Opt("lastName"),
                            IsMissingAgreementProperties = IsMissingAnyOf(customerItems, "addressZipcode", "email", "phone", "firstName")
                        };
                        handle(applicantNr, a);
                    }
                };

                var state = new ApplicationState
                {
                    Token = token,
                    ApplicationNr = app.ApplicationNr,
                    NrOfApplicants = app.NrOfApplicants,
                    IsForcedBankAccountDataSharing = app.IsForcedBankAccountDataSharing
                };
                if (app.IsActive && !app.IsPartiallyApproved)
                {
                    state.IsActive = true;
                    if (app.State != null && app.CreditCheckStatus == "Accepted")
                    {
                        if (app.State.IsPendingOrWaitingForAdditionalQuestions)
                        {
                            string notApplicableMsg;
                            var hasAdditionalLoanOffer = AdditionalLoanSupport.HasAdditionalLoanOffer(app.ApplicationNr, app.CreditCheckStatus, app.CurrentCreditDecision, out notApplicableMsg);
                            if (!hasAdditionalLoanOffer.HasValue)
                            {
                                throw new Exception(notApplicableMsg); //Should not be possible to end up here in this case
                            }
                            if (!app.State.IsPendingExternalAdditionalQuestions)
                            {
                                state.ActiveState = new ApplicationState.ActiveStateModel
                                {
                                    ShouldAnswerAdditionalQuestions = true,
                                    AdditionalQuestionsData = new ApplicationState.ActiveStateModel.AdditionalQuestionsInitialDataModel
                                    {
                                        IsAdditionalLoanOffer = hasAdditionalLoanOffer.Value
                                    }
                                };
                                loadApplicants((applicantNr, a) =>
                                {
                                    if (applicantNr == 1)
                                    {
                                        state.ActiveState.AdditionalQuestionsData.Applicant1 = a;
                                    }
                                    else if (applicantNr == 2)
                                    {
                                        state.ActiveState.AdditionalQuestionsData.Applicant2 = a;
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                });
                            }
                            else
                            {
                                const string ApplicationSourceType = "UnsecuredLoanApplication";

                                var hasAnsweredQuestions = customerClient.SetupCustomerKycDefaults(new SetupCustomerKycDefaultsRequest
                                {
                                    CustomerIds = customerIdByApplicantNr.Value.Values.ToList(),
                                    OnlyThisSourceId = app.ApplicationNr,
                                    OnlyThisSourceType = ApplicationSourceType

                                }).HaveAllCustomersAnsweredQuestions;

                                if (hasAnsweredQuestions)
                                {
                                    //Remove the marker now that we have external answers and reload the state to move on to signing
                                    c.GetConnection().Execute(
                                        "update CreditApplicationItem set [Value] = 'false' where ApplicationNr = @applicationNr and GroupName = 'application' and [Name] = 'isPendingExternalKycQuestions'",
                                        param: new { applicationNr = app.ApplicationNr });

                                    KycScreenApplication(app.ApplicationNr);

                                    return GetApplicationStateI(token);
                                }
                                else
                                {
                                    var applicationUrl = new Uri(envSettings.ApplicationWrapperUrlPattern.Replace("{token}", token)).ToString();
                                    var session = customerClient.CreateKycQuestionSession(new CreateKycQuestionSessionRequest
                                    {
                                        CustomerIds = customerIdByApplicantNr.Value.Keys.Select(applicantNr => customerIdByApplicantNr.Value[applicantNr]).ToList(),
                                        Language = app.UserLanguage ?? clientConfiguration.Country.GetBaseLanguage(),
                                        QuestionsRelationType = "Credit_UnsecuredLoan",
                                        RedirectUrl = applicationUrl,
                                        SlidingExpirationHours = 4,
                                        SourceType = ApplicationSourceType,
                                        SourceId = app.ApplicationNr,
                                        SourceDescription = $"Unsecured loan application {app.ApplicationNr}"
                                    });
                                    state.ActiveState = new ApplicationState.ActiveStateModel
                                    {
                                        ShouldAnswerExternalAdditionalQuestions = app.State.IsPendingExternalAdditionalQuestions,
                                        ExternalAdditionalQuestionsData = new ApplicationState.ActiveStateModel.ExternalAdditionalQuestionsInitialDataModel
                                        {
                                            RedirectUrl = serviceRegistry.ExternalServiceUrl("nCustomerPages", $"n/public-kyc/questions-session/{session.SessionId}").ToString()
                                        }
                                    };
                                }
                            }
                        }
                        else if (app.State.IsPendingOrWaitingForSignature)
                        {
                            if (app.CustomerCheckStatus != "Rejected")
                            {
                                state.ActiveState = new ApplicationState.ActiveStateModel
                                {
                                    ShouldSignAgreements = true,
                                    AgreementsData = new ApplicationState.ActiveStateModel.AgreementInitialModel()
                                };

                                bool isMissingAgreementData = false;
                                loadApplicants((applicantNr, a) =>
                                {
                                    if (a.IsMissingAgreementProperties)
                                        isMissingAgreementData = true;
                                    if (applicantNr == 1)
                                    {
                                        state.ActiveState.AgreementsData.Applicant1 = a;
                                    }
                                    else if (applicantNr == 2)
                                    {
                                        state.ActiveState.AgreementsData.Applicant2 = a;
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                });

                                if (isMissingAgreementData)
                                {
                                    state.ActiveState = new ApplicationState.ActiveStateModel
                                    {
                                        IsWaitingForClient = true
                                    };
                                }
                                else
                                {
                                    var applicantNrsThatSigned = GetCustomersThatSignedAgreement(
                                        app.ApplicationNr,
                                        app.NrOfApplicants,
                                        app.SignedAgreementKeyItems.Select(x => Tuple.Create(x.GroupName, x.Value)).ToList());
                                    foreach (var applicantNr in Enumerable.Range(1, app.NrOfApplicants))
                                    {
                                        var hasSigned = applicantNrsThatSigned.Contains(applicantNr);
                                        if (applicantNr == 1)
                                        {
                                            state.ActiveState.AgreementsData.HasApplicant1SignedAgreement = hasSigned;
                                        }
                                        else if (applicantNr == 2)
                                        {
                                            state.ActiveState.AgreementsData.HasApplicant2SignedAgreement = hasSigned;
                                        }
                                        else
                                            throw new NotImplementedException();
                                    }
                                }
                            }
                            else
                            {
                                state.ActiveState = new ApplicationState.ActiveStateModel
                                {
                                    IsWaitingForClient = true
                                };
                            }
                        }
                        else if (app.AgreementStatus == "Accepted")
                        {
                            var isBankAccountDataSharingEnabled = clientConfiguration.IsFeatureEnabled("ntech.feature.bankAccountDataSharing");
                            var isOnDocumentStep = app.DocumentCheckStatus == "Initial" && !app.HasAttachedDocuments;

                            var missingAnyDocumentSourceSelections = isBankAccountDataSharingEnabled
                                ? app.DocumentSourceStatusApplicant1 == "pendingSelection" || (app.NrOfApplicants > 1 && app.DocumentSourceStatusApplicant2 == "pendingSelection")
                                : false;

                            var hasApplicant1ChosenDataSource = app.DocumentSourceStatusApplicant1 != "pendingSelection";
                            var hasApplicant2ChosenDataSource = app.DocumentSourceStatusApplicant2 != "pendingSelection";

                            var isWatingForDocumentUpload = isBankAccountDataSharingEnabled
                                ? hasApplicant1ChosenDataSource || (app.NrOfApplicants > 1 && hasApplicant2ChosenDataSource)
                                : true;

                            if (isOnDocumentStep)
                            {
                                state.ActiveState = new ApplicationState.ActiveStateModel
                                {
                                    IsWaitingForClient = false, //Both the user and the client can handle this so sort of unclear. Probably not
                                    IsAwaitingFinalApproval = false,
                                    ShouldChooseDocumentSource = missingAnyDocumentSourceSelections,
                                    IsWatingForDocumentUpload = isWatingForDocumentUpload,
                                    IsWaitingForSharedAccountDataCallback =
                                        app.DocumentSourceStatusApplicant1 == "shareAccount" && string.IsNullOrWhiteSpace(app.SharedAccountDataPdfArchiveKey1)
                                        ||
                                        app.DocumentSourceStatusApplicant2 == "shareAccount" && string.IsNullOrWhiteSpace(app.SharedAccountDataPdfArchiveKey2),
                                    DocumentUploadData = new ApplicationState.ActiveStateModel.DocumentUploadDataModel
                                    {

                                    },
                                    DocumentSourceData = missingAnyDocumentSourceSelections ? new ApplicationState.ActiveStateModel.DocumentSourceDataModel
                                    {
                                        HasApplicant1ChosenDataSource = hasApplicant1ChosenDataSource,
                                        HasApplicant2ChosenDataSource = hasApplicant2ChosenDataSource,
                                    } : null
                                };
                                loadApplicants((applicantNr, a) =>
                                {
                                    if (applicantNr == 1)
                                    {
                                        state.ActiveState.DocumentUploadData.Applicant1 = new ApplicationState.ActiveStateModel.DocumentUploadDataModel.Applicant
                                        {
                                            CivicRegNr = a.CivicRegNr,
                                            FirstName = a.FirstName,
                                            LastName = a.LastName,
                                            SharedAccountDataPdfPreviewArchiveKey = app.SharedAccountDataPdfArchiveKey1
                                        };
                                    }
                                    else if (applicantNr == 2)
                                    {
                                        state.ActiveState.DocumentUploadData.Applicant2 = new ApplicationState.ActiveStateModel.DocumentUploadDataModel.Applicant
                                        {
                                            CivicRegNr = a.CivicRegNr,
                                            FirstName = a.FirstName,
                                            LastName = a.LastName,
                                            SharedAccountDataPdfPreviewArchiveKey = app.SharedAccountDataPdfArchiveKey2
                                        };
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                });
                            }
                            else
                            {
                                state.ActiveState = new ApplicationState.ActiveStateModel
                                {
                                    IsWaitingForClient = true,
                                    IsAwaitingFinalApproval = true
                                };
                            }
                        }
                        else
                        {
                            state.ActiveState = new ApplicationState.ActiveStateModel
                            {
                                IsWaitingForClient = true
                            };
                        }
                    }
                    else
                    {
                        state.ActiveState = new ApplicationState.ActiveStateModel
                        {
                            IsWaitingForClient = true
                        };
                    }
                }
                else if (app.IsActive && app.IsPartiallyApproved)
                {
                    //From the customers perspective this is closer to reality than saying it's being processed still
                    state.ClosedState = new ApplicationState.ClosedStateModel
                    {
                        WasAccepted = true
                    };
                }
                else
                {
                    state.ClosedState = new ApplicationState.ClosedStateModel
                    {
                        WasAccepted = app.IsFinalDecisionMade
                    };
                }
                return state;
            }
        }

        private bool IsMissingAnyOf(IDictionary<string, string> d, params string[] keyNames)
        {
            foreach (var k in keyNames)
                if (string.IsNullOrWhiteSpace(d.Opt(k)))
                    return true;
            return false;
        }

        public ApplicationState ApplyAdditionalQuestionAnswers(string token, UlLegacyKycAnswersModel answers, string userLanguage)
        {
            string applicationNr;
            using (var c = preCreditContextFactory.CreateExtended())
            {
                var app = ApplicationsByTokenQuery(c, token)
                        .Select(x => new
                        {
                            x.IsActive,
                            x.ApplicationNr,
                            x.NrOfApplicants,
                            x.AgreementStatus,
                            x.CreditCheckStatus
                        })
                        .SingleOrDefault();

                if (app == null)
                    throw new NTechCoreWebserviceException("No such application") { ErrorHttpStatusCode = 400, IsUserFacing = true };

                if (!app.IsActive)
                    throw new NTechCoreWebserviceException("Application is not active") { ErrorHttpStatusCode = 400, IsUserFacing = true };

                if (app.AgreementStatus == "Accepted")
                {
                    throw new NTechCoreWebserviceException("Agreement already signed") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                }

                if (app.CreditCheckStatus != "Accepted")
                {
                    throw new NTechCoreWebserviceException("CreditCheck is not accepted") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                }

                applicationNr = app.ApplicationNr;
            }

            string failedMessage;

            if (!agreementSignatureService.TryHandleAnswersToAdditionalQuestions(applicationNr, null, answers, out failedMessage, userLanguage: userLanguage))
            {
                throw new NTechCoreWebserviceException(failedMessage) { ErrorHttpStatusCode = 400, IsUserFacing = true };
            }
            else
            {
                return GetApplicationState(token);
            }
        }

        private void KycScreenApplication(string applicationNr)
        {
            void LogException(Exception ex)
            {
                var hasCustomReason = (ex as NTechCoreWebserviceException)?.ErrorCode == "kycScreenFailedWithReason";
                UpdateApplicationOnKycScreenFailure(applicationNr, hasCustomReason
                    ? ex.Message
                    : "Kyc screen failed");
                if (!hasCustomReason)
                {
                    loggingService.Error(ex, $"Kyc screen failed for {applicationNr}");
                }
            }

            try
            {
                var applicants = applicationInfoService.GetApplicationApplicants(applicationNr);
                foreach (var applicant in applicants.CustomerIdByApplicantNr)
                {
                    var applicantNr = applicant.Key;
                    var customerId = applicant.Value;

                    KycScreenCustomer(applicantNr, customerId);
                };
            }
            catch (Exception ex)
            {
                if (IsKycErrorHandlingSupressed)
                {
                    throw;
                }
                LogException(ex);
            }
        }

        private void KycScreenCustomer(int applicantNr, int customerId)
        {
            var result = customerClient.KycScreenNew(new[] { customerId }.ToHashSetShared(), clock.Today, true);
            var failedReason = result.Opt(customerId);

            if (failedReason != null)
            {
                throw new NTechCoreWebserviceException(CreditApplicationComment.CleanCommentText($"Kyc screen failed for applicant {applicantNr}: {failedReason}"))
                {
                    ErrorCode = "kycScreenFailedWithReason"
                };
            }
        }

        private void UpdateApplicationOnKycScreenFailure(string applicationNr, string message)
        {
            using (var context = preCreditContextFactory.CreateExtended())
            {
                var comment = new CreditApplicationComment
                {
                    ApplicationNr = applicationNr,
                    CommentText = message,
                    CommentDate = context.CoreClock.Now,
                    CommentById = context.CurrentUserId,
                    EventType = "KycScreenFailed",
                };
                context.FillInfrastructureFields(comment);
                context.AddCreditApplicationComments(comment);

                var app = context.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == applicationNr);
                app.CustomerCheckStatus = "Rejected";

                context.SaveChanges();
            }
        }

        public class DocumentCheckAttachRequest
        {
            public string token { get; set; }
            public List<File> Files { get; set; }
            public class File
            {
                public int ApplicantNr { get; set; }
                public string FileName { get; set; }
                public string MimeType { get; set; }
                public string ArchiveKey { get; set; }
            }
        }

        public class DocumentSourceRequest
        {
            public string token { get; set; }
            public string applicantNr { get; set; }
            public string sourceCode { get; set; }
        }
    }
}