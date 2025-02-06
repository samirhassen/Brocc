using System;
using System.Collections.Generic;
using System.Linq;

namespace NTechSignicat.Services
{
    public class SignatureSession
    {
        public const int CurrentFormatVersionNr = 4; //Update whenever the data format changes. Make sure it always increases

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
        public string ServerToServerCallbackUrl { get; set; }
        public List<string> RawSignaturePackages { get; set; }
        public List<DocumentModel> Documents { get; set; }
        public List<SignedDocumentCombination> SignedDocumentCombinations { get; set; }

        public bool IsSingleDocumentSession()
        {
            return FormatVersionNr >= 4 && Documents.Count == 1 && SignedDocumentCombinations.Count == 1;
        }

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

        public SignatureSessionStateCode GetState()
        {
            SignatureSessionStateCode s;
            return Enum.TryParse(this.SessionStateCode, out s) ? s : SignatureSessionStateCode.Broken;
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

        public void SetState(SignatureSessionStateCode stateCode, string stateMessage = null)
        {
            this.SessionStateCode = stateCode.ToString();
            this.SessionStateMessage = stateMessage;
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
            public bool UsesTestReplacementCivicRegNrs { get; set; } //Has no effect in production
        }
    }
}