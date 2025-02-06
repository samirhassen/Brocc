using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using NTech.Shared.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public abstract class SignicatSignatureServiceBase : ISignicatSignatureService
    {
        private readonly IDocumentDatabaseService documentDatabaseService;
        private readonly IDocumentService documentService;
        private readonly INEnv env;
        private const string SignatureSessionKeySpace = "SignatureSessionsV1";
        private const string RequestIdToSignatureSessionKeySpace = "SignatureRequestIdToSessionIdV1";
        private const string AlternateSessionKeyToSessionIdKeySpace = "SignatureAlternateSessionKeyToSessionIdV1";

        public SignicatSignatureServiceBase(IDocumentDatabaseService documentDatabaseService, IDocumentService documentService, INEnv env)
        {
            this.documentDatabaseService = documentDatabaseService;
            this.documentService = documentService;
            this.env = env;
        }

        protected abstract Task<CreateSignatureResponse> CreateSignatureRequest(Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr, List<SignaturePdf> pdfs);

        protected abstract Task<Dictionary<string, DocumentSignatureResult>> GetSignatureStatusAndSignedDocumentUris(SignatureSession session);

        protected abstract Task<byte[]> PackageAsPdf(string requestId, List<string> documentUris);

        protected string GetSignatureMethod(SignicatLoginMethodCode signicatLoginMethod)
        {
            if (signicatLoginMethod == SignicatLoginMethodCode.FinnishTrustNetwork)
                return "ftn-sign";
            else if (signicatLoginMethod == SignicatLoginMethodCode.SwedishBankId)
                return "sbid-sign";
            else
                throw new NotImplementedException();
        }

        public async Task<SignatureSession> CreatePdfsSignatureRequest(
            Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr,
            List<SignaturePdf> pdfs,
            List<SignedDocumentCombination> signedDocumentCombinations,
            Uri redirectAfterSuccessUrl,
            Uri redirectAfterFailedUrl,
            Dictionary<string, string> customData = null,
            string alternateSessionKey = null,
            Uri serverToServerCallbackUrl = null)
        {
            var now = DateTime.UtcNow;
            foreach (var pdf in pdfs)
            {
                if (pdf.DocumentId == null)
                    throw new Exception("Missing documentId");
            }
            var firstDocument = pdfs.First();
            var id = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(length: 10);
            var s = new SignatureSession
            {
                Id = id,
                FormatVersionNr = SignatureSession.CurrentFormatVersionNr,
                ExpirationDateUtc = now,
                StartDateUtc = now,
                SigningCustomersByApplicantNr = new Dictionary<int, SignatureSession.SigningCustomer>(),
                RedirectAfterFailedUrl = redirectAfterFailedUrl.ToString().Replace("{{SessionId}}", id),
                RedirectAfterSuccessUrl = redirectAfterSuccessUrl.ToString().Replace("{{SessionId}}", id),
                ServerToServerCallbackUrl = serverToServerCallbackUrl?.ToString(),
                CustomData = customData ?? new Dictionary<string, string>(),
                SignedDocumentCombinations = signedDocumentCombinations.Select(x => new SignatureSession.SignedDocumentCombination
                {
                    CombinationFileName = x.CombinationFileName,
                    CombinationId = x.CombinationId,
                    RequestDocumentIds = x.DocumentIds
                })?.ToList()
            };
            s.SetState(SignatureSessionStateCode.PendingAllSignatures);

            var r = await this.CreateSignatureRequest(signingCustomersByApplicantNr, pdfs);

            const string PdfMimeType = "application/pdf";

            s.Documents = r.Documents.Select(x => new SignatureSession.DocumentModel
            {
                DocumentFileName = x.Pdf.PdfDisplayFileName,
                DocumentMimeType = PdfMimeType,
                DocumentSdsCode = x.SdsCode,
                RequestDocumentId = x.Pdf.DocumentId
            }).ToList();

            s.SigningCustomersByApplicantNr = r.SigningCustomersByApplicantNr;
            s.SignicatRequestId = r.RequestId;

            this.documentDatabaseService.Set(SignatureSessionKeySpace, s.Id, s, TimeSpan.FromDays(7));
            this.documentDatabaseService.Set(RequestIdToSignatureSessionKeySpace, r.RequestId, s.Id, TimeSpan.FromDays(14));
            if (!string.IsNullOrWhiteSpace(alternateSessionKey))
                this.documentDatabaseService.Set(AlternateSessionKeyToSessionIdKeySpace, alternateSessionKey, s.Id, TimeSpan.FromDays(14));
            return s;
        }

        public async Task<SignatureSession> CreatePdfSignatureRequest(
            Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr,
            byte[] pdfBytes, string pdfDisplayFileName,
            Uri redirectAfterSuccessUrl,
            Uri redirectAfterFailedUrl,
            Dictionary<string, string> customData = null,
            string alternateSessionKey = null,
            Uri serverToServerCallbackUrl = null)
        {
            var pdfs = new List<SignaturePdf>
                {
                    new SignaturePdf
                    {
                        PdfBytes = pdfBytes,
                        PdfDisplayFileName = pdfDisplayFileName,
                        DocumentId = "document1"
                    }
                };

            var combinations = new List<SignedDocumentCombination>
            {
                new SignedDocumentCombination
                {
                    CombinationFileName = pdfDisplayFileName,
                    CombinationId = "document1",
                    DocumentIds = new List<string> { "document1" }
                }
            };

            return await CreatePdfsSignatureRequest(signingCustomersByApplicantNr, pdfs, combinations,
                redirectAfterSuccessUrl, redirectAfterFailedUrl, customData: customData,
                alternateSessionKey: alternateSessionKey, serverToServerCallbackUrl: serverToServerCallbackUrl);
        }

        public class CreateSignatureResponse
        {
            public string RequestId { get; set; }
            public List<Document> Documents { get; set; }
            public Dictionary<int, SignatureSession.SigningCustomer> SigningCustomersByApplicantNr { get; set; }

            public class Document
            {
                public SignaturePdf Pdf { get; set; }
                public string SdsCode { get; set; }
            }
        }

        public SignatureSession GetSessionByAlternateKey(string alternateSessionKey)
        {
            var sessionId = this.documentDatabaseService.Get<string>(AlternateSessionKeyToSessionIdKeySpace, alternateSessionKey);
            if (sessionId == null)
                return null;
            return this.GetSession(sessionId);
        }

        public SignatureSession GetSession(string sessionId)
        {
            return this.documentDatabaseService.Get<SignatureSession>(SignatureSessionKeySpace, sessionId);
        }

        public async Task<SignatureSession> HandleSignatureCallback(string requestId, string taskId, string status)
        {
            var sessionId = this.documentDatabaseService.Get<string>(RequestIdToSignatureSessionKeySpace, requestId);
            var session = GetSession(sessionId);

            if (session == null)
                return null;

            if (status == "taskpostponed")
                return session; //We ignore this status

            var state = session.GetState();

            if (status == "taskcomplete" && (state == SignatureSessionStateCode.PendingAllSignatures || state == SignatureSessionStateCode.PendingSomeSignatures))
            {
                var applicantNr = int.Parse(taskId);
                var c = session.SigningCustomersByApplicantNr[applicantNr];
                c.SignedDateUtc = DateTime.UtcNow;
                
                var signatureResultByTaskId = await GetSignatureStatusAndSignedDocumentUris(session);

                if (signatureResultByTaskId[taskId].TaskStatus != "completed")
                {
                    session.SetState(SignatureSessionStateCode.Failed, $"Applicant {taskId} returned with status completed but signicat claims status {signatureResultByTaskId[taskId].TaskStatus}");
                }
                else if (session.SigningCustomersByApplicantNr.All(x => x.Value.SignedDateUtc.HasValue))
                {
                    var allDocumentUrls = signatureResultByTaskId.Values.SelectMany(x => x.SignedDocumentUriByDocumentId.Values).Distinct().ToList();
                    List<string> GetUrisByDocumentId(string documentId)
                    {
                        var result = new List<string>();
                        foreach(var d in signatureResultByTaskId.Values)
                        {
                            if(d.SignedDocumentUriByDocumentId.ContainsKey(documentId))
                                result.Add(d.SignedDocumentUriByDocumentId[documentId]);
                        }
                        return result;
                    }
                    if (allDocumentUrls.Count > 0)
                    {
                        bool isValid = true;

                        if (isValid)
                        {
                            if (session.FormatVersionNr >= 4)
                            {
                                if (env.OptionalSetting("ntech.signicat.disablesignatureverification") != "true")
                                {
                                    var (isOk, civicNrs) = await this.VerifyIsSignedByAtLeastThesePersons(session, allDocumentUrls.ToList());
                                    if (!isOk)
                                    {
                                        session.SetState(SignatureSessionStateCode.Failed, $"Signed civic regnrs do not match requested. Actual: {string.Join(", ", civicNrs.Select(x => x.NormalizedValue))}");
                                        isValid = false;
                                    }
                                }
                                foreach (var signedCombination in session.SignedDocumentCombinations)
                                {
                                    var altUris = signedCombination
                                            .RequestDocumentIds
                                            .SelectMany(GetUrisByDocumentId)
                                            .Distinct()
                                            .ToList();
                                    if (altUris.Count > 0)
                                    {
                                        var altSignedDocumentBytes = await PackageAsPdf(requestId, altUris);
                                        var altDocument = documentService.Store(altSignedDocumentBytes, "application/pdf", TimeSpan.FromDays(14),
                                            documentDownloadName: signedCombination.CombinationFileName,
                                            customData: new Dictionary<string, string> { { "signatureSessionId", session.Id }, { "combinationId", signedCombination.CombinationId } });
                                        signedCombination.SignedDocumentKey = altDocument.DocumentKey;
                                    }
                                }
                                if (session.IsSingleDocumentSession())
                                {
                                    //Single document type calls but with newer sessions. This cannot be removed unless ISignicatSignatureService.CreatePdfSignatureRequest is removed
                                    session.SignedDocumentKey = session.SignedDocumentCombinations.Single().SignedDocumentKey;
                                }
                            }
                            else
                            {
                                throw new Exception("Session version no longer supported");
                            }

                            session.SetState(SignatureSessionStateCode.SignaturesSuccessful);
                        }
                    }
                    else
                    {
                        session.SetState(SignatureSessionStateCode.Failed, "No signed document link provided by signicat");
                    }
                }
                else
                {
                    session.SetState(SignatureSessionStateCode.PendingSomeSignatures);
                }
            }
            else if (status == "taskcanceled" && (state == SignatureSessionStateCode.PendingAllSignatures || state == SignatureSessionStateCode.PendingSomeSignatures))
            {
                //We used to log this as a failure but the session is apparently still live at signicat so we just leave it as pending.
            }

            this.documentDatabaseService.Set(SignatureSessionKeySpace, session.Id, session, TimeSpan.FromDays(7));

            return session;
        }

        public SignatureSession CancelSignatureSession(string sessionId)
        {
            var session = GetSession(sessionId);

            if (session == null)
                return null;

            var state = session.GetState();

            if (state != SignatureSessionStateCode.Cancelled && state != SignatureSessionStateCode.SignaturesSuccessful && state != SignatureSessionStateCode.Failed)
            {
                session.SetState(SignatureSessionStateCode.Cancelled, $"Session cancelled");
                this.documentDatabaseService.Set(SignatureSessionKeySpace, session.Id, session, TimeSpan.FromDays(7));
            }

            return session;
        }

        protected abstract Task<(bool isOk, List<ICivicRegNumber> signedByCivicRegNrs)> VerifyIsSignedByAtLeastThesePersons(SignatureSession session, List<string> resultUris);
    }

    public class DocumentSignatureResult
    {
        public string TaskStatus { get; set; }
        public Dictionary<string, string> SignedDocumentUriByDocumentId { get; set; }
    }
}