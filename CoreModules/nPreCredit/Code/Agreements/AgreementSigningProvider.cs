using Newtonsoft.Json;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using ICoreDocumentClient = NTech.Core.Module.Shared.Clients.IDocumentClient;

namespace nPreCredit.Code
{

    public class AgreementSigningProvider
    {
        private IClock clock;
        private ICoreClock coreClock;
        private readonly IApplicationCommentServiceComposable applicationCommentService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly EncryptionService encryptionService;
        private readonly LoanAgreementPdfBuilderFactory agreementPdfBuilderFactory;

        public AgreementSigningProvider(
            INTechCurrentUserMetadata ntechCurrentUserMetadata,
            ICombinedClock clock,
            IApplicationCommentServiceComposable applicationCommentService,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            IPreCreditContextFactoryService preCreditContextFactory,
            EncryptionService encryptionService,
            LoanAgreementPdfBuilderFactory agreementPdfBuilderFactory)
        {
            this.clock = clock;
            this.coreClock = clock;
            this.applicationCommentService = applicationCommentService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.preCreditContextFactory = preCreditContextFactory;
            this.encryptionService = encryptionService;
            this.agreementPdfBuilderFactory = agreementPdfBuilderFactory;
        }

        public class AgreementSigningLinkInfo
        {
            public Uri SignUrl { get; set; }
            public string AgreementPdfFileName { get; set; }
            public string AgreementDocumentKey { get; set; }
            public string SigningSessionKey { get; set; }
            public SignatureProviderCode Provider { get; set; }
        }

        public bool TryCreateAgreementPdf(string applicationNr, int applicantNr, out byte[] pdfBytes, out bool isAdditionalLoanOffer, out string failedMessage, Action<string> observeAgreementDataHash = null)
        {
            using (var context = preCreditContextFactory.CreateExtended())
            {
                string notApplicableMsg;
                var tmp = AdditionalLoanSupport.HasAdditionalLoanOffer(applicationNr, context, out notApplicableMsg);
                if (!tmp.HasValue)
                {
                    pdfBytes = null;
                    isAdditionalLoanOffer = false;
                    failedMessage = notApplicableMsg;
                    return false;
                }

                isAdditionalLoanOffer = tmp.Value;
            }

            var pdfBuilder = agreementPdfBuilderFactory.Create(isAdditionalLoanOffer);

            if (!isAdditionalLoanOffer)
            {
                AddCreditNrIfNeeded(applicationNr, "SendAgreementLink");
            }

            var isAllowed = pdfBuilder.IsCreateAgreementPdfAllowed(applicationNr, out failedMessage);
            if (!isAllowed)
            {
                pdfBytes = null;
                return false;
            }

            return pdfBuilder.TryCreateAgreementPdf(out pdfBytes, out failedMessage, applicationNr, skipAllowedCheck: true, observeAgreementDataHash: observeAgreementDataHash);
        }

        public void AddCreditNrIfNeeded(string applicationNr, string stepName)
        {
            //Add a creditnumber if needed
            var appModel = partialCreditApplicationModelRepository.Get(
                applicationNr,
                new PartialCreditApplicationModelRequest
                {
                    ApplicationFields = new List<string> { "creditnr" }
                });
            if (appModel.Application.Get("creditnr").StringValue.Optional == null)
            {
                var c = new CreditClient();
                var creditNr = c.NewCreditNumber();

                var repo = new UpdateCreditApplicationRepository(coreClock, preCreditContextFactory, encryptionService);
                repo.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                {
                    InformationMetadata = ntechCurrentUserMetadata.InformationMetadata,
                    StepName = stepName,
                    UpdatedByUserId = ntechCurrentUserMetadata.UserId,
                    Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                    {
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "application",
                            Name = "creditnr",
                            IsSensitive = false,
                            Value = creditNr
                        }
                    }
                });
            }
        }

        private const string SignatureTokenType = "SignInitialCreditAgreement";
        public static string SignedDocumentItemName => AgreementSigningProviderShared.SignedDocumentItemName;

        public bool TryCreateAndPossiblySendAgreementLink(byte[] agreementPdfBytes, string agreementDataHash, string applicationNr, int applicantNr, out string failedMessage, out string signUrl, string urlToken, bool allowInvalidEmail = false)
        {
            var tokenString = CreditApplicationOneTimeToken.GenerateUniqueToken();
            var customerClient = new PreCreditCustomerClient();
            var m = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" });

            var applicantNrByCustomerId = new Dictionary<int, int>();
            m.DoForEachApplicant(an =>
            {
                var cid = m.Applicant(an).Get("customerId").IntValue.Required;
                applicantNrByCustomerId[cid] = an;
            });

            var customerItemsByApplicantNr = customerClient.BulkFetchPropertiesByCustomerIdsSimple(new HashSet<int>(applicantNrByCustomerId.Keys), "civicRegNr", "email")
                .ToDictionary(x => applicantNrByCustomerId[x.Key], x => x.Value);

            var items = customerItemsByApplicantNr[applicantNr];

            if (!items.ContainsKey("email") || !items.ContainsKey("civicRegNr"))
            {
                failedMessage = "email or civicRegnr missing from the customer";
                signUrl = null;
                return false;
            }

            var email = items["email"];
            var civicRegNr = items["civicRegNr"];

            if (!allowInvalidEmail)
            {
                var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                if (!emailValidator.IsValid(email))
                {
                    failedMessage = "email invalid from the customer";
                    signUrl = null;
                    return false;
                }
            }

            var now = this.clock.Now;
            string newExternalSignatureRequestId = null;

            var signingLinkInfo = ConstructNewAgreementSigningLink(
                applicationNr,
                applicantNr,
                agreementPdfBytes,
                tokenString,
                customerItemsByApplicantNr.Keys.ToDictionary(x => x, x => customerItemsByApplicantNr[x]["civicRegNr"]),
                agreementDataHash,
                urlToken,
                x => newExternalSignatureRequestId = x);

            signUrl = signingLinkInfo.SignUrl.AbsoluteUri;

            using (var c = new PreCreditContextExtended(ntechCurrentUserMetadata.UserId, CoreClock.SharedInstance, ntechCurrentUserMetadata.InformationMetadata))
            {
                var token = new CreditApplicationOneTimeToken
                {
                    ApplicationNr = applicationNr,
                    CreationDate = now,
                    ChangedDate = now,
                    ChangedById = ntechCurrentUserMetadata.UserId,
                    InformationMetaData = ntechCurrentUserMetadata.InformationMetadata,
                    Token = tokenString,
                    TokenType = SignatureTokenType,
                    ValidUntilDate = now.AddDays(2),
                    TokenExtraData = JsonConvert.SerializeObject(new SignatureTokenExtraDataModel { status = "Pending", applicantNr = applicantNr, signingSessionKey = signingLinkInfo.SigningSessionKey, agreementDocumentKey = signingLinkInfo.AgreementDocumentKey, providerName = signingLinkInfo.Provider.ToString(), agreementDataHash = agreementDataHash })
                };

                var commentText = $"Agreement link created for direct signature by applicant {applicantNr}{(newExternalSignatureRequestId == null ? " reusing signature session" : $" with new signature session {newExternalSignatureRequestId}")}";
                var attachment = signingLinkInfo.AgreementDocumentKey == null ? null : CommentAttachment.CreateFileFromArchiveKey(signingLinkInfo.AgreementDocumentKey, "application/pdf", signingLinkInfo.AgreementPdfFileName);
                if (!applicationCommentService.TryAddCommentComposable(
                    token.ApplicationNr, commentText, "AgreementSentForSigning", attachment, out failedMessage, c))
                {
                    return false;
                }

                c.CreditApplicationOneTimeTokens.Add(token);

                c.SaveChanges();
            }

            failedMessage = null;
            return true;
        }

        public class SignatureTokenExtraDataModel
        {
            public string signingSessionKey { get; set; }
            public string agreementDocumentKey { get; set; }
            public string providerName { get; set; }
            public string status { get; set; }
            public int applicantNr { get; set; }
            public string agreementDataHash { get; set; }
        }

        protected static Uri AppendQueryStringParam(Uri uri, string name, string value)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[name] = value;
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        public static string GetAlternateSignicatKey(string applicationNr) =>
            AgreementSigningProviderShared.GetAlternateSignicatKey(applicationNr);

        public AgreementSigningLinkInfo ConstructNewAgreementSigningLink(
            string applicationNr, int applicantNr, byte[] agreementPdfBytes, string tokenString,
            Dictionary<int, string> civicRegNrByApplicantNr,
            string agreementDataHash, string urlToken, Action<string> observeNewSignatureRequestId)
        {
            var providerCodeRaw = NEnv.SignatureProvider;
            if (!providerCodeRaw.HasValue)
                throw new Exception("Missing SignatureProvider");
            var providerCode = providerCodeRaw.Value;

            Uri CreateRedirectUrl(string relativePath)
            {
                return NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", relativePath,
                    Tuple.Create("token", urlToken),
                    Tuple.Create("signatureSessionKey", "{localSessionId}"));
            };

            var successUserRedirectUrl = CreateRedirectUrl("application-wrapper-direct-signed-ok");
            var failedUserRedirectUrl = CreateRedirectUrl("application-wrapper-direct-signed-failed");

            switch (providerCode)
            {
                case SignatureProviderCode.signicat2:
                    {
                        ICustomerClient customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);

                        using (var context = preCreditContextFactory.CreateExtended())
                        {
                            var sessionId = KeyValueStoreService.GetValueComposable(context, applicationNr, "ActiveSignatureSession");
                            if (sessionId != null)
                            {
                                var existingSession = customerClient.GetElectronicIdSignatureSession(sessionId, false)?.Session;
                                if (existingSession != null && existingSession.ClosedDate == null)
                                {
                                    return new AgreementSigningLinkInfo
                                    {
                                        Provider = providerCode,
                                        SigningSessionKey = existingSession.Id,
                                        SignUrl = new Uri(existingSession.GetActiveSignatureUrlBySignerNr()[applicantNr])
                                    };
                                }
                            }

                            ICoreDocumentClient docClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                            var file = docClient.ArchiveStore(agreementPdfBytes, "application/pdf", "Agreement.pdf");

                            var request = new SingleDocumentSignatureRequestUnvalidated
                            {
                                DocumentToSignArchiveKey = file,
                                DocumentToSignFileName = "Agreeement.pdf",
                                RedirectAfterSuccessUrl = successUserRedirectUrl?.ToString(),
                                RedirectAfterFailedUrl = failedUserRedirectUrl?.ToString(),
                                SigningCustomers = civicRegNrByApplicantNr.Select(customer => new SingleDocumentSignatureRequestUnvalidated.SigningCustomer { SignerNr = customer.Key, CivicRegNr = customer.Value }).ToList(),
                                CustomData = new Dictionary<string, string>
                                        {
                                            { "applicationNr", applicationNr },
                                            { "SignatureSessionType", Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.UnsecuredLoanAgreementSignatureV1.ToString() }
                                        }
                            };

                            var session = customerClient.CreateElectronicIdSignatureSession(request);

                            KeyValueStoreService.SetValueComposable(context, applicationNr, "ActiveSignatureSession", session.Id);
                            context.SaveChanges();

                            return new AgreementSigningLinkInfo
                            {
                                Provider = providerCode,
                                SigningSessionKey = session.Id,
                                SignUrl = new Uri(session.GetActiveSignatureUrlBySignerNr()[applicantNr])
                            };
                        };
                    }
                case SignatureProviderCode.signicat:
                    {
                        //Check if there is a signature session active on this application already and use that if possible (so both applicants sign the same document)
                        var sc = SignicatSigningClientFactory.CreateClient();
                        var session = sc.GetSignatureSessionByAlternateKey(GetAlternateSignicatKey(applicationNr), false);
                        var newHash = agreementDataHash;
                        var currentHash = session?.CustomData?.Opt("agreementHash");
                        var isSameAgreement = newHash == currentHash;

                        Func<SignicatSigningClient.SignatureSession> createNewSignatureSession = () =>
                            {
                                var request = new Clients.SignicatSigningClient.StartSingleDocumentSignatureSessionRequest
                                {
                                    AlternateSessionKey = GetAlternateSignicatKey(applicationNr),
                                    RedirectAfterFailedUrl = failedUserRedirectUrl?.ToString(),
                                    RedirectAfterSuccessUrl = successUserRedirectUrl?.ToString(),
                                    PdfBytesBase64 = Convert.ToBase64String(agreementPdfBytes),
                                    PdfDisplayFileName = "Agreement.pdf",
                                    SigningCustomersByApplicantNr = new Dictionary<int, Clients.SignicatSigningClient.StartSingleDocumentSignatureSessionRequest.Customer>(),
                                    CustomData = new Dictionary<string, string>
                                        {
                                            { "applicationNr", applicationNr },
                                            { "agreementHash", newHash },
                                            { "SignatureSessionType", Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.UnsecuredLoanAgreementSignatureV1.ToString() }
                                        }
                                };

                                foreach (var a in civicRegNrByApplicantNr)
                                {
                                    request.SigningCustomersByApplicantNr[a.Key] = new Clients.SignicatSigningClient.StartSingleDocumentSignatureSessionRequest.Customer
                                    {
                                        ApplicantNr = a.Key,
                                        CivicRegNr = a.Value,
                                        SignicatLoginMethod = sc.GetElectronicIdLoginMethod()
                                    };
                                }

                                var newSession = sc.StartSingleDocumentSignatureSession(
                                    request);

                                if (newSession != null)
                                    observeNewSignatureRequestId?.Invoke(newSession.SignicatRequestId);

                                return newSession;
                            };

                        if (applicantNr == 1)
                        {
                            var activeSignatureUrl = session?.GetActiveSignatureUrlForApplicant(applicantNr);
                            if (activeSignatureUrl != null && isSameAgreement)
                                return new AgreementSigningLinkInfo
                                {
                                    Provider = providerCode,
                                    SigningSessionKey = session.Id,
                                    SignUrl = new Uri(activeSignatureUrl)
                                };
                            else
                            {
                                session = createNewSignatureSession();
                                return new AgreementSigningLinkInfo
                                {
                                    Provider = providerCode,
                                    SigningSessionKey = session.Id,
                                    SignUrl = new Uri(session.GetActiveSignatureUrlForApplicant(applicantNr))
                                };
                            }
                        }
                        else
                        {
                            //The troublesome case here is if the first user has not signed. This will happen if they already signed a separate document. In this case we want to back up and have them both re-sign.
                            if (session != null && session.SessionStateCode == "PendingSomeSignatures" && session.HasSigned(1) && isSameAgreement)
                            {
                                //Standard case. The main applicant signed this document and now it's the co applicants turn
                                return new AgreementSigningLinkInfo
                                {
                                    Provider = providerCode,
                                    SigningSessionKey = session.Id,
                                    SignUrl = new Uri(session.GetActiveSignatureUrlForApplicant(applicantNr))
                                };
                            }
                            else
                            {
                                //This is a problem case since here we want to go back to the first user and have them sign instead.
                                //Remove all the signed documents before this to cause the signature process to restart.
                                using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, CoreClock.SharedInstance))
                                {
                                    CancelSignatureSessionIfAny(applicationNr, providerCode == SignatureProviderCode.signicat, context);
                                    context.SaveChanges();
                                }
                                throw new SignatureMustRestartFromFirstUserException();
                            }
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static void CancelSignatureSessionIfAny(string applicationNr, bool usesSignicat, PreCreditContextExtended context)
        {
            var signatureTokens = context.CreditApplicationOneTimeTokens.Where(x => x.ApplicationNr == applicationNr && x.TokenType == SignatureTokenType && !x.RemovedBy.HasValue).ToList();
            foreach (var signatureToken in signatureTokens)
            {
                signatureToken.RemovedBy = context.CurrentUserId;
                signatureToken.RemovedDate = context.Clock.Now;
            }

            var documentKeys = context.CreditApplicationItems.Where(x => x.ApplicationNr == applicationNr && x.Name == SignedDocumentItemName).ToList();
            foreach (var d in documentKeys)
                context.CreditApplicationItems.Remove(d);

            if (usesSignicat)
            {
                var sc = SignicatSigningClientFactory.CreateClient();
                sc.TryCancelSignatureSessionByAlternateKey(AgreementSigningProvider.GetAlternateSignicatKey(applicationNr), out var _);
            }

            else
            {
                var sessionId = KeyValueStoreService.GetValueComposable(context, applicationNr, "ActiveSignatureSession");
                ICustomerClient customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                var existingSession = customerClient.GetElectronicIdSignatureSession(sessionId, false)?.Session;
            }
        }

        public class SignatureStatusModel
        {
            public int? ApplicantNr { get; set; }
            public bool IsMissingSession { get; set; }
            public bool IsPendingSignature { get; set; }
            public bool IsFailed { get; set; }
            public string FailedMessage { get; set; }
            public bool IsSuccess { get; set; }
            public string SignedDocumentUrl { get; set; }
            public string SignedDocumentArchiveKey { get; set; }
            public ISet<int> ApplicantsNrsThatHaveSigned { get; set; }
            public DateTimeOffset SuccessDate { get; set; }
        }

        public SignatureStatusModel GetSignatureStatusOnCallback(string applicationNr, string token, string signatureSessionKey)
        {
            SignatureTokenExtraDataModel tokenState = null;
            using (var context = new PreCreditContext())
            {
                var wrapperToken = context.CreditApplicationOneTimeTokens.Where(x => x.Token == token).Select(x => x.TokenExtraData).FirstOrDefault();
                if (wrapperToken != null)
                {
                    var latestTokens = context
                        .CreditApplicationOneTimeTokens
                        .Where(x => x.ApplicationNr == applicationNr && x.TokenType == SignatureTokenType && !x.RemovedBy.HasValue)
                        .OrderByDescending(x => x.Timestamp)
                        .Select(x => x.TokenExtraData)
                        .Take(2)
                        .ToList();
                    foreach (var d in latestTokens)
                    {
                        var t = JsonConvert.DeserializeObject<SignatureTokenExtraDataModel>(d);
                        if (t != null && t.signingSessionKey == signatureSessionKey)
                        {
                            tokenState = t;
                            break;
                        }
                    }
                }
            }

            var r = new SignatureStatusModel
            {
                ApplicantNr = tokenState?.applicantNr,
                ApplicantsNrsThatHaveSigned = new HashSet<int>()
            };

            var providerCode = tokenState?.providerName;
            if (string.IsNullOrWhiteSpace(providerCode))
                throw new Exception("Missing providerCode");

            if (tokenState == null)
            {
                r.IsFailed = true;
                r.FailedMessage = "Missing signature token";
            }
            else if (providerCode == SignatureProviderCode.signicat2.ToString() && NEnv.SignatureProvider == SignatureProviderCode.signicat2)
            {
                using (var context = preCreditContextFactory.CreateExtended())
                {
                    ICustomerClient customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);

                    var sessionId = KeyValueStoreService.GetValueComposable(context, applicationNr, "ActiveSignatureSession");
                    if (sessionId != null)
                    {
                        var session = customerClient.GetElectronicIdSignatureSession(sessionId, false)?.Session;
                        if (session == null)
                        {
                            r.IsFailed = true;
                            r.FailedMessage = "Missing signature token";
                        }
                        else
                        {
                            if (session.SignedPdf.ArchiveKey != null)
                            {
                                r.IsSuccess = true;
                                r.SignedDocumentArchiveKey = session.SignedPdf.ArchiveKey;
                                r.SuccessDate = clock.Now;
                            }
                            else if (session.ClosedDate.HasValue)
                            {
                                r.IsFailed = true;
                                r.FailedMessage = session.ClosedMessage;
                            }
                            else
                            {
                                r.IsPendingSignature = true;
                            }

                            r.ApplicantsNrsThatHaveSigned.AddRange(session.GetSignedByApplicantNrs());
                        }
                    }
                    return r;
                };
            }
            else if (providerCode == SignatureProviderCode.signicat.ToString() && NEnv.SignatureProvider == SignatureProviderCode.signicat)
            {
                if (tokenState.status == "Pending")
                {
                    var sc = SignicatSigningClientFactory.CreateClient();
                    var s = sc.GetSignatureSessionByAlternateKey(GetAlternateSignicatKey(applicationNr), true);
                    if (s == null)
                    {
                        r.IsFailed = true;
                        r.FailedMessage = "Missing signature token";
                    }
                    else if (tokenState.agreementDataHash == s.CustomData?.Opt("agreementHash"))
                    {
                        if (s.SessionStateCode == "SignaturesSuccessful")
                        {
                            r.IsSuccess = true;
                            r.SignedDocumentUrl = s.SignedDocumentKey != null ? s.GetSignedDocumentUrl(new ServiceRegistryLegacy(NEnv.ServiceRegistry)).ToString() : null;
                            r.SuccessDate = clock.Now;
                        }
                        else if (s.SessionStateCode == "Cancelled" || s.SessionStateCode == "Failed")
                        {
                            r.IsFailed = true;
                            r.FailedMessage = s.SessionStateCode == "Cancelled" ? "Signature session was cancelled" : s.SessionStateMessage;
                        }
                        else
                        {
                            r.IsPendingSignature = true;
                        }
                        foreach (var a in s.SigningCustomersByApplicantNr.Where(x => x.Value.SignedDateUtc.HasValue))
                        {
                            r.ApplicantsNrsThatHaveSigned.Add(a.Key);
                        }
                    }
                    else
                    {
                        r.IsFailed = true;
                        r.FailedMessage = "Agreement has changed. Signature must be restarted";
                    }
                }
                else
                {
                    r.IsFailed = true;
                    r.FailedMessage = $"Invalid token state. Expected pending signature but was: {tokenState.status}";
                }
            }
            else
            {
                r.IsFailed = true;
                r.FailedMessage = $"Provider {providerCode} not supported. The signature session needs to be restarted";
            }

            return r;
        }
    }

    public class SignatureMustRestartFromFirstUserException : Exception
    {
        public SignatureMustRestartFromFirstUserException()
        {
        }

        public SignatureMustRestartFromFirstUserException(string message) : base(message)
        {
        }

        public SignatureMustRestartFromFirstUserException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SignatureMustRestartFromFirstUserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}