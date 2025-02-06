using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static nPreCredit.Code.Clients.SignicatSigningClient;

namespace nPreCredit.Code.Clients
{

    public class SignicatSigningClient : ISignicatSigningClientReadOnly
    {
        private readonly ServiceClient serviceClient;
        private readonly IClientConfigurationCore clientConfiguration;

        public SignicatSigningClient(ServiceClientFactory serviceClientFactory, INHttpServiceUser serviceUser,
            IClientConfigurationCore clientConfiguration)
        {
            serviceClient = serviceClientFactory.CreateClient(serviceUser, "NTechSignicat");
            this.clientConfiguration = clientConfiguration;
        }

        public LoginSession StartLoginSession(StartLoginSessionRequest request)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/start-login-session", request), x => x.ParseJsonAs<LoginSession>()));
        }

        public LoginSession GetLoginSession(GetLoginSessionRequest request)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/get-login-session", request), x => x.ParseJsonAs<LoginSession>()));
        }

        public LoginSession CompleteLoginSession(CompleteLoginSessionRequest request)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/complete-login-session", request), x => x.ParseJsonAs<LoginSession>()));
        }

        public SignatureSession StartSingleDocumentSignatureSession(StartSingleDocumentSignatureSessionRequest request)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/start-signature-session", request), x => x.ParseJsonAs<SignatureSession>()));
        }

        public SignatureSession StartMultiDocumentSignatureSession(StartMultiDocumentSignatureSessionRequest request)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/start-multidocument-signature-session", request), x => x.ParseJsonAs<SignatureSession>()));
        }

        public SignatureSession GetSignatureSession(string sessionId)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/get-signature-session", new { sessionId }), x => x.ParseJsonAs<SignatureSession>()));
        }

        public bool TryCancelSignatureSessionByAlternateKey(string alternateSessionKey, out SignatureSession cancelledSession)
        {
            cancelledSession = serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/cancel-signature-session", new { AlternateSessionKey = alternateSessionKey }), x =>
                    {
                        if (x.IsSuccessStatusCode)
                            return x.ParseJsonAs<SignatureSession>();
                        else if (x.StatusCode == 400)
                            return null;
                        else
                        {
                            x.EnsureSuccessStatusCode(); //will throw
                            return null;
                        }
                    }));
            return cancelledSession != null;
        }

        public bool TryCancelSignatureSessionBySessionId(string sessionId, out SignatureSession cancelledSession)
        {
            cancelledSession = serviceClient.ToSync(() =>
                serviceClient
                    .Call(x => x.PostJson("api/cancel-signature-session", new { SessionId = sessionId }), x =>
                    {
                        if (x.IsSuccessStatusCode)
                            return x.ParseJsonAs<SignatureSession>();
                        else if (x.StatusCode == 400)
                            return null;
                        else
                        {
                            x.EnsureSuccessStatusCode(); //will throw
                            return null;
                        }
                    }));
            return cancelledSession != null;
        }

        public SignatureSession GetSignatureSessionByAlternateKey(string alternateSessionKey, bool allowCancelledOrFailedSession)
        {
            var session = serviceClient.ToSync(() =>
                serviceClient
                    .Call(
                        x => x.PostJson("api/get-signature-session", new { alternateSessionKey }),
                        x => x.ParseJsonAs<SignatureSession>()));

            if (!allowCancelledOrFailedSession && (session?.SessionStateCode?.IsOneOfIgnoreCase("Cancelled", "Failed") ?? false))
                return null;
            return session;
        }

        public StoredDocument GetDocument(string documentKey)
        {
            return serviceClient.ToSync(() =>
                serviceClient
                    .Call(
                        x => x.PostJson("api/document", new { documentKey }),
                        x => x.ParseJsonAs<StoredDocument>()));
        }

        public string GetElectronicIdLoginMethod()
        {
            var countryIsoCode = clientConfiguration.Country.BaseCountry;
            if (countryIsoCode == "FI")
                return "FinnishTrustNetwork";
            else if (countryIsoCode == "SE")
                return "SwedishBankId";
            else
                throw new NotImplementedException();
        }

        public void DownloadAndHandleSignedDocument(string documentId, SignatureSession s, IDocumentClient dc, Action<string, string> withArchiveKeyAndFilename)
        {
            var combination = documentId == null
                ? s.SignedDocumentCombinations.Single()
                : s.SignedDocumentCombinations.Single(x => x.CombinationId == documentId);
            var document = GetDocument(combination.SignedDocumentKey);
            var archiveKey = dc.ArchiveStore(Convert.FromBase64String(document.DocumentDataBase64), document.DocumentMimeType, combination.CombinationFileName);
            withArchiveKeyAndFilename(archiveKey, combination.CombinationFileName);
        }

        public class StoredDocument
        {
            public string DocumentDataBase64 { get; set; }
            public string DocumentMimeType { get; set; }
            public string DocumentDownloadName { get; set; }
            public string DocumentKey { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }

        public class CompleteLoginSessionRequest
        {
            public string SessionId { get; set; }
            public string Token { get; set; }
        }

        public class GetLoginSessionRequest
        {
            public string SessionId { get; set; }
        }

        public class StartLoginSessionRequest
        {
            public string ExpectedCivicRegNr { get; set; }

            public List<string> LoginMethods { get; set; }

            public string RedirectAfterSuccessUrl { get; set; }

            public string RedirectAfterFailedUrl { get; set; }

            public Dictionary<string, string> CustomData { get; set; }
        }

        public class LoginSession
        {
            public string ExpectedCivicRegNr { get; set; }
            public string ExpectedCivicRegNrCountryIsoCode { get; set; }
            public string Id { get; set; }
            public string SessionStateCode { get; set; }
            public DateTime ExpirationDateUtc { get; set; }
            public DateTime StartDateUtc { get; set; }
            public DateTime? CallbackDateUtc { get; set; }
            public DateTime? LoginDateUtc { get; set; }
            public string SignicatReturnUrl { get; set; }
            public string SignicatInitialUrl { get; set; }
            public TokenSetModel Tokens { get; set; }
            public UserInfoModel UserInfo { get; set; }
            public string FailedCode { get; set; }
            public string FailedMessage { get; set; }
            public string RedirectAfterSuccessUrl { get; set; }
            public string RedirectAfterFailedUrl { get; set; }
            public string OneTimeInternalLoginToken { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }

        public class TokenSetModel
        {
            public string AccessToken { get; set; }
            public string IdToken { get; set; }
            public DateTime? ExpiresDateUtc { get; set; }
            public ISet<string> Scopes { get; set; }
        }

        public class UserInfoModel
        {
            public string CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class StartMultiDocumentSignatureSessionRequest : StartSignatureSessionRequestBase
        {
            public List<PdfModel> Pdfs { get; set; }

            public class PdfModel
            {
                public string PdfId { get; set; }

                public string PdfBytesBase64 { get; set; }

                public string PdfDisplayFileName { get; set; }
            }

            public List<SignedCombination> SignedCombinations { get; set; }

            public class SignedCombination
            {
                public string CombinationId { get; set; }
                public List<string> PdfIds { get; set; }
                public string CombinationFileName { get; set; }
            }
        }

        public class StartSingleDocumentSignatureSessionRequest : StartSignatureSessionRequestBase
        {
            public string PdfBytesBase64 { get; set; }

            public string PdfDisplayFileName { get; set; }
        }

        public class StartSignatureSessionRequestBase
        {
            public Dictionary<int, Customer> SigningCustomersByApplicantNr { get; set; }

            public string RedirectAfterSuccessUrl { get; set; }

            public string RedirectAfterFailedUrl { get; set; }
            public string ServerToServerCallbackUrl { get; set; }
            public string AlternateSessionKey { get; set; }
            public Dictionary<string, string> CustomData { get; set; }

            public class Customer
            {
                public string CivicRegNr { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public int ApplicantNr { get; set; }
                public string UserLanguage { get; set; }
                public string SignicatLoginMethod { get; set; }
            }
        }

        public class SignatureSession
        {
            public string Id { get; set; }
            public int? FormatVersionNr { get; set; }
            public string SessionStateCode { get; set; }
            public string SessionStateMessage { get; set; }
            public DateTime ExpirationDateUtc { get; set; }
            public DateTime StartDateUtc { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
            public string DocumentSdsCode { get; set; }
            public string DocumentMimeType { get; set; }
            public string DocumentFileName { get; set; }
            public string SignedDocumentKey { get; set; }
            public string SignicatRequestId { get; set; }
            public Dictionary<int, SigningCustomer> SigningCustomersByApplicantNr { get; set; }
            public string RedirectAfterFailedUrl { get; set; }
            public string RedirectAfterSuccessUrl { get; set; }

            public List<DocumentModel> Documents { get; set; }
            public List<SignedDocumentCombination> SignedDocumentCombinations { get; set; }

            public class DocumentModel
            {
                public string DocumentSdsCode { get; set; }
                public string RequestDocumentId { get; set; }
                public string DocumentMimeType { get; set; }
                public string DocumentFileName { get; set; }
            }

            public class SignedDocumentCombination
            {
                public string CombinationId { get; set; }
                public List<string> RequestDocumentIds { get; set; }
                public string SignedDocumentKey { get; set; }
                public string CombinationFileName { get; set; }
            }

            public bool HasSigned(int applicantNr)
            {
                var c = SigningCustomersByApplicantNr.Opt(applicantNr);
                return c == null ? false : c.SignedDateUtc.HasValue;
            }

            public Uri GetSignedDocumentUrl(INTechServiceRegistry serviceRegistry)
            {
                return SignedDocumentKey == null ? null : serviceRegistry.ExternalServiceUrl("NTechSignicat", $"api/document/{SignedDocumentKey}");
            }

            public string GetNextSignatureUrl()
            {
                return SigningCustomersByApplicantNr
                            ?.Values
                            ?.Where(x => !x.SignedDateUtc.HasValue)
                            ?.OrderBy(x => x.ApplicantNr)
                            ?.FirstOrDefault()
                            ?.SignicatSignatureUrl;
            }

            public string GetActiveSignatureUrlForApplicant(int applicantNr)
            {
                if (!(SessionStateCode ?? "").IsOneOf("PendingAllSignatures", "PendingSomeSignatures"))
                    return null;
                var c = SigningCustomersByApplicantNr?.Opt(applicantNr);
                if (c == null || (c != null && c.SignedDateUtc.HasValue))
                    return null;
                return c?.SignicatSignatureUrl;
            }

            public class SigningCustomer
            {
                public int ApplicantNr { get; set; }
                public string UserLanguage { get; set; }
                public string SignicatTaskId { get; set; }
                public string SignicatSignatureUrl { get; set; }
                public DateTime? SignedDateUtc { get; set; }
                public string CivicRegNr { get; set; }
                public string CivicRegNrCountry { get; set; }
            }
        }
    }
    public interface ISignicatSigningClientReadOnly
    {
        SignatureSession GetSignatureSessionByAlternateKey(string alternateSessionKey, bool allowCancelledOrFailedSession);
    }
}