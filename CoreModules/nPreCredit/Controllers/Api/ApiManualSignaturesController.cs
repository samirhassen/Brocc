using nPreCredit.Code;
using nPreCredit.Code.ElectronicSignatures;
using NTech;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    public class ApiManualSignaturesController : NController
    {
        [Route("api/ManualSignatures/CreateDocuments")]
        [HttpPost]
        public ActionResult CreateDocuments(string dataUrl, string filename, string civicRegNr, string commentText)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing filename");

            byte[] filedata;
            string mimeType;
            if (!FileUtilities.TryParseDataUrl(dataUrl, out mimeType, out filedata))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid file");

            var dc = new nDocumentClient();
            var archiveKey = dc.ArchiveStore(filedata, mimeType, filename);
            var sr = NEnv.ServiceRegistry;

            var request = new SingleDocumentSignatureRequest
            {
                DocumentToSignArchiveKey = archiveKey,
                DocumentToSignFileName = filename,
                SigningCustomers = new List<SingleDocumentSignatureRequest.SigningCustomer>()
                {
                    new SingleDocumentSignatureRequest.SigningCustomer
                    {
                        SignerNr = 1,
                        CivicRegNr = civicRegNr,
                        FirstName = "Signer",
                        LastName = "1"
                    }
                },
                ServerToServerCallbackUrl = sr.Internal.ServiceUrl("nPreCredit", "api/Signatures/Signal-Session-Event").ToString(),
                CustomData = new Dictionary<string, string>
                                        {
                                              { "SignatureSessionType", ApiSignaturePostbackController.SignatureSessionTypeCode.ManualSignature.ToString() }
                                        },
                RedirectAfterSuccessUrl = sr.External.ServiceUrl("nCustomerPages", "signature-result/success").ToString(),
                RedirectAfterFailedUrl = sr.External.ServiceUrl("nCustomerPages", "signature-result/failure").ToString()
            };

            var provider = new ElectronicSignatureProvider(Clock);

            if (GetSignatureProviderCode() == SignatureProviderCode.signicat2)
            {
                request.RedirectAfterSuccessUrl = sr.External.ServiceUrl("nCustomerPages", "signature-result-redirect/{localSessionId}/success").ToString();
                request.RedirectAfterFailedUrl = sr.External.ServiceUrl("nCustomerPages", "signature-result-redirect/{localSessionId}/failure").ToString();
            }

            var session = provider.CreateSingleDocumentSignatureSession(request);

            var archiveDocumentUrl = NEnv.ServiceRegistry.External.ServiceUrl("nDocument", "Archive/Fetch", Tuple.Create("key", archiveKey)).ToString();
            var now = ClockFactory.SharedInstance.Now.DateTime;

            using (var context = new PreCreditContext())
            {
                var c = new ManualSignature
                {
                    SignatureSessionId = session.Id,
                    CreationDate = now,
                    IsHandled = false,
                    UnsignedDocumentArchiveKey = archiveKey,
                    CommentText = commentText
                };
                context.ManualSignatures.Add(c);
                context.SaveChanges();
            }

            return Json2(new
            {
                ArchiveDocumentUrl = archiveDocumentUrl,
                SessionId = session.Id,
                SignatureUrl = session.GetActiveSignatureUrlBySignerNr().Opt(1),
                CreationDate = now.ToShortDateString()
            });
        }

        [Route("api/ManualSignatures/DeleteDocuments")]
        [HttpPost]
        public ActionResult DeleteDocuments(string sessionId)
        {
            var provider = new ElectronicSignatureProvider(Clock);

            provider.TryCloseSession(sessionId);

            var now = ClockFactory.SharedInstance.Now.DateTime;
            using (var context = new PreCreditContext())
            {
                var manualSignatures = context.ManualSignatures.Where(f => f.SignatureSessionId == sessionId).ToList();
                var anyRemoved = false;
                manualSignatures.ForEach(a =>
                {
                    a.IsRemoved = true;
                    a.RemovedDate = now;
                    anyRemoved = true;
                });
                if (anyRemoved)
                    context.SaveChanges();
            }
            return new EmptyResult();
        }

        [Route("api/ManualSignatures/GetDocuments")]
        [HttpPost]
        public ActionResult GetDocuments(bool signedDocuments)
        {
            var provider = new ElectronicSignatureProvider(Clock);

            string GetSignatureUrl(ManualSignature x)
            {
                if (x.SignedDocumentArchiveKey == null && !x.IsHandled.GetValueOrDefault() && !x.IsRemoved.GetValueOrDefault())
                {
                    var session = provider.GetCommonSignatureSession(x.SignatureSessionId, true);
                    if (session == null)
                        return null;
                    return session.GetActiveSignatureUrlBySignerNr().Opt(1);
                }
                return null;
            }

            using (var context = new PreCreditContext())
            {
                List<ManualSignature> manualSignatures;
                if (signedDocuments)
                    manualSignatures = context.ManualSignatures.Where(f => !string.IsNullOrEmpty(f.SignedDocumentArchiveKey) && (f.IsHandled == false || f.IsHandled == null)).ToList();
                else
                    manualSignatures = context.ManualSignatures.Where(f => string.IsNullOrEmpty(f.SignedDocumentArchiveKey) && (f.IsRemoved == false || f.IsRemoved == null)).ToList();

                var result = manualSignatures
                                .Select(x => new
                                {
                                    SignatureUrl = GetSignatureUrl(x),
                                    x.CommentText,
                                    x.CreationDate,
                                    x.HandleByUser,
                                    x.HandledDate,
                                    x.IsHandled,
                                    x.IsRemoved,
                                    x.RemovedDate,
                                    x.SignatureSessionId,
                                    x.SignedDate,
                                    UnSignedDocumentArchiveUrl = x.UnsignedDocumentArchiveKey == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("nDocument", "Archive/Fetch", Tuple.Create("key", x.UnsignedDocumentArchiveKey)).ToString(),
                                    SignedDocumentArchiveUrl = x.SignedDocumentArchiveKey == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("nDocument", "Archive/Fetch", Tuple.Create("key", x.SignedDocumentArchiveKey)).ToString()
                                })
                                    .ToList();
                return (Json2(result));
            }
        }

        [Route("api/ManualSignatures/HandleDocuments")]
        [HttpPost]
        public ActionResult HandleDocuments(string sessionId)
        {
            var now = ClockFactory.SharedInstance.Now.DateTime;
            using (var context = new PreCreditContext())
            {
                var manualSignatures = context.ManualSignatures.Where(f => f.SignatureSessionId == sessionId).ToList();
                manualSignatures.ForEach(a =>
                {
                    a.IsHandled = true;
                    a.HandledDate = now;
                    a.HandleByUser = CurrentUserId;
                    context.SaveChanges();
                });
            }
            return new EmptyResult();
        }

        private SignatureProviderCode GetSignatureProviderCode()
        {
            var s = NTechEnvironment.Instance.Setting("ntech.eidsignatureprovider", true).Trim()?.ToLowerInvariant();
            return s == null
                ? throw new Exception("Missing setting 'ntech.eidsignatureprovider'")
                : (SignatureProviderCode)Enum.Parse(typeof(SignatureProviderCode), s, true);
        }
    }
}