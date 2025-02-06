using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Clients
{
    public class SignicatSigningClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "NTechSignicat";

        public SignatureSession StartSingleDocumentSignatureSession(StartSingleDocumentSignatureSessionRequest request)
        {
            return Begin()
                .PostJson("api/start-signature-session", request)
                .ParseJsonAs<SignatureSession>();
        }

        public SignatureSession GetSignatureSession(string sessionId)
        {
            return Begin()
                .PostJson("api/get-signature-session", new { sessionId })
                .ParseJsonAs<SignatureSession>();
        }

        public bool TryCancelSignatureSessionByAlternateKey(string alternateSessionKey, out SignatureSession cancelledSession)
        {
            var r = Begin().PostJson("api/cancel-signature-session", new { AlternateSessionKey = alternateSessionKey });
            if (r.IsSuccessStatusCode)
                cancelledSession = r.ParseJsonAs<SignatureSession>();
            else if (r.StatusCode == 400)
                cancelledSession = null;
            else
            {
                cancelledSession = null;
                r.EnsureSuccessStatusCode(); //will throw
            }

            return cancelledSession != null;
        }

        public bool TryCancelSignatureSessionBySessionId(string sessionId, out SignatureSession cancelledSession)
        {
            var r = Begin().PostJson("api/cancel-signature-session", new { SessionId = sessionId });
            if (r.IsSuccessStatusCode)
                cancelledSession = r.ParseJsonAs<SignatureSession>();
            else if (r.StatusCode == 400)
                cancelledSession = null;
            else
            {
                cancelledSession = null;
                r.EnsureSuccessStatusCode(); //will throw
            }

            return cancelledSession != null;
        }

        public SignatureSession GetSignatureSessionByAlternateKey(string alternateSessionKey, bool allowCancelledOrFailedSession)
        {
            var session = Begin()
                .PostJson("api/get-signature-session", new { alternateSessionKey })
                .ParseJsonAs<SignatureSession>();
            if (!allowCancelledOrFailedSession && (session?.SessionStateCode?.IsOneOfIgnoreCase("Cancelled", "Failed") ?? false))
                return null;
            return session;
        }

        public StoredDocument GetDocument(string documentKey)
        {
            return Begin()
                .PostJson("api/document", new { documentKey })
                .ParseJsonAs<StoredDocument>();
        }

        public string GetElectronicIdLoginMethod()
        {
            var countryIsoCode = NEnv.ClientCfg.Country.BaseCountry;
            if (countryIsoCode == "FI")
                return "FinnishTrustNetwork";
            else if (countryIsoCode == "SE")
                return "SwedishBankId";
            else
                throw new NotImplementedException();
        }

        public void DownloadAndHandleSignedDocument(string documentId, SignatureSession s, DocumentClient dc, Action<string, string> withArchiveKeyAndFilename)
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

        public class TokenSetModel
        {
            public string AccessToken { get; set; }
            public string IdToken { get; set; }
            public DateTime? ExpiresDateUtc { get; set; }
            public ISet<string> Scopes { get; set; }
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

            public Uri GetSignedDocumentUrl()
            {
                return SignedDocumentKey == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("NTechSignicat", $"api/document/{SignedDocumentKey}");
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
}