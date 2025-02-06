using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public interface ISignicatSignatureService
    {
        Task<SignatureSession> CreatePdfSignatureRequest(
            Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr,
            byte[] pdfBytes, string pdfDisplayFileName,
            Uri redirectAfterSuccessUrl,
            Uri redirectAfterFailedUrl,
            Dictionary<string, string> customData = null,
            string alternateSessionKey = null,
            Uri serverToServerCallbackUrl = null);

        Task<SignatureSession> CreatePdfsSignatureRequest(
            Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr,
            List<SignaturePdf> pdfs,
            List<SignedDocumentCombination> signedDocumentCombinations,
            Uri redirectAfterSuccessUrl,
            Uri redirectAfterFailedUrl,
            Dictionary<string, string> customData = null,
            string alternateSessionKey = null,
            Uri serverToServerCallbackUrl = null);

        Task<SignatureSession> HandleSignatureCallback(string requestId, string taskId, string status);

        SignatureSession GetSession(string sessionId);

        SignatureSession GetSessionByAlternateKey(string alternateSessionKey);

        SignatureSession CancelSignatureSession(string sessionId);
    }

    public class SignaturePdf
    {
        /// <summary>
        /// Only required if using alternate combinations. This will be assigned automatically if not set
        /// </summary>
        public string DocumentId { get; set; }

        public string PdfDisplayFileName { get; set; }
        public byte[] PdfBytes { get; set; }
    }

    public class SignedDocumentCombination
    {
        public string CombinationId { get; set; }
        public List<string> DocumentIds { get; set; }
        public string CombinationFileName { get; set; }
    }
}