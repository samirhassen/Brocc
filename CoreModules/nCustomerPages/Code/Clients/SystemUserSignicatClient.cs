using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomerPages.Code.Clients
{
    public class SystemUserSignicatClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "NTechSignicat";

        public SignatureSession StartSignatureSession(StartSignatureSessionRequest request)
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

        public class StoredDocument
        {
            public string DocumentDataBase64 { get; set; }
            public string DocumentMimeType { get; set; }
            public string DocumentDownloadName { get; set; }
            public string DocumentKey { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }

        public class StartSignatureSessionRequest
        {
            public Dictionary<int, Customer> SigningCustomersByApplicantNr { get; set; }

            public string PdfBytesBase64 { get; set; }

            public string PdfDisplayFileName { get; set; }

            public string RedirectAfterSuccessUrl { get; set; }

            public string RedirectAfterFailedUrl { get; set; }

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

            public Uri GetSignedDocumentUrl()
            {
                return SignedDocumentKey == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("NTechSignicat", $"api/document/{SignedDocumentKey}");
            }

            public string GetNextSignatureUrl()
            {
                return SigningCustomersByApplicantNr
                            .Values
                            .Where(x => !x.SignedDateUtc.HasValue)
                            .OrderBy(x => x.ApplicantNr)
                            .FirstOrDefault()
                            ?.SignicatSignatureUrl;
            }

            public class SigningCustomer
            {
                public int ApplicantNr { get; set; }
                public string UserLanguage { get; set; }
                public string SignicatTaskId { get; set; }
                public string SignicatSignatureUrl { get; set; }
                public DateTime? SignedDateUtc { get; set; }
            }
        }
    }
}