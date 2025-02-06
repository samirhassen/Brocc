using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NTechSignicat.Models;
using NTechSignicat.Services;

namespace NTechSignicat.Controllers
{
    [Route("api")]
    [ApiController]
    public class SignatureApiController : Controller
    {
        private readonly ISignicatSignatureService signatureService;
        private readonly SignicatLoginMethodValidator signicatLoginMethodValidator;

        public SignatureApiController(ISignicatSignatureService signatureService, SignicatLoginMethodValidator signicatLoginMethodValidator)
        {
            this.signatureService = signatureService;
            this.signicatLoginMethodValidator = signicatLoginMethodValidator;
        }

        [Route("get-signature-session")]
        [HttpPost]
        public IActionResult GetSignatureSession([FromBody] GetSignatureSessionRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SessionId))
                return Json(this.signatureService.GetSession(request?.SessionId));
            else if (!string.IsNullOrWhiteSpace(request.AlternateSessionKey))
                return Json(this.signatureService.GetSessionByAlternateKey(request?.AlternateSessionKey));
            else
                return StatusCode(400, new { errorMessage = "Must specify either SessionId or AlternateSessionKey" });
        }

        [Route("cancel-signature-session")]
        [HttpPost]
        public IActionResult CancelSignatureSession([FromBody] CancelSignatureSessionRequest request)
        {
            string sessionId;
            if (!string.IsNullOrWhiteSpace(request.AlternateSessionKey))
                sessionId = this.signatureService.GetSessionByAlternateKey(request.AlternateSessionKey)?.Id;
            else
                sessionId = request.SessionId;

            if (!string.IsNullOrWhiteSpace(sessionId))
                return Json(this.signatureService.CancelSignatureSession(sessionId));
            else
                return StatusCode(400, new { errorMessage = "Must specify either SessionId or AlternateSessionKey or session missing with that alternate key." });
        }

        private async Task<IActionResult> StartSignatureSessionI<T>(T request, Func<Dictionary<int, SignatureRequestCustomer>, Task<SignatureSession>> createSession) where T : StartSignatureSessionRequestBase
        {
            var r = new Dictionary<int, SignatureRequestCustomer>();
            foreach (var a in request.SigningCustomersByApplicantNr)
            {
                SignicatLoginMethodValidator.ValidatedParameters validatedParameters;
                if (!signicatLoginMethodValidator.TryValidate(a.Value.CivicRegNr, new List<string> { a.Value.SignicatLoginMethod }, out validatedParameters))
                    return BadRequest();

                r[a.Key] = new SignatureRequestCustomer
                {
                    ApplicantNr = a.Value.ApplicantNr,
                    CivicRegNr = validatedParameters.ExpectedCivicRegNr,
                    SignicatLoginMethod = validatedParameters.LoginMethods.Single(),
                    FirstName = a.Value.FirstName,
                    LastName = a.Value.LastName,
                    UserLanguage = a.Value.UserLanguage
                };
            }

            var session = await createSession(r);

            return Json(session);
        }

        [Route("start-signature-session")]
        [HttpPost]
        public async Task<IActionResult> StartSignatureSession([FromBody] StartSingleDocumentSignatureSessionRequest request)
        {
            return await StartSignatureSessionI(request, r => this.signatureService.CreatePdfSignatureRequest(r,
                Convert.FromBase64String(request.PdfBytesBase64), request.PdfDisplayFileName,
                new Uri(request.RedirectAfterSuccessUrl), new Uri(request.RedirectAfterFailedUrl),
                customData: request.CustomData,
                alternateSessionKey: request.AlternateSessionKey,
                serverToServerCallbackUrl: string.IsNullOrWhiteSpace(request.ServerToServerCallbackUrl) ? null : new Uri(request.ServerToServerCallbackUrl)));
        }

        [Route("start-multidocument-signature-session")]
        [HttpPost]
        public async Task<IActionResult> StartMultiDocumentSignatureSession([FromBody] StartMultiDocumentSignatureSessionRequest request)
        {
            var pdfs = request.Pdfs.Select(x => new SignaturePdf
            {
                DocumentId = x.PdfId,
                PdfBytes = Convert.FromBase64String(x.PdfBytesBase64),
                PdfDisplayFileName = x.PdfDisplayFileName
            }).ToList();

            var signedDocumentCombinations = request.SignedCombinations.Select(x => new SignedDocumentCombination
            {
                CombinationId = x.CombinationId,
                CombinationFileName = x.CombinationFileName,
                DocumentIds = x.PdfIds
            })?.ToList();

            return await StartSignatureSessionI(request, r => this.signatureService.CreatePdfsSignatureRequest(r,
                pdfs, signedDocumentCombinations,
                new Uri(request.RedirectAfterSuccessUrl), new Uri(request.RedirectAfterFailedUrl),
                customData: request.CustomData,
                alternateSessionKey: request.AlternateSessionKey,
                serverToServerCallbackUrl: string.IsNullOrWhiteSpace(request.ServerToServerCallbackUrl) ? null : new Uri(request.ServerToServerCallbackUrl)));
        }

        public class GetSignatureSessionRequest
        {
            public string SessionId { get; set; }
            public string AlternateSessionKey { get; set; }
        }

        public class CancelSignatureSessionRequest
        {
            public string SessionId { get; set; }
            public string AlternateSessionKey { get; set; }
        }

        public class StartSingleDocumentSignatureSessionRequest : StartSignatureSessionRequestBase
        {
            [Required]
            public string PdfBytesBase64 { get; set; }

            public string PdfDisplayFileName { get; set; }
        }

        public class StartMultiDocumentSignatureSessionRequest : StartSignatureSessionRequestBase
        {
            [Required]
            public List<PdfModel> Pdfs { get; set; }

            [Required]
            public List<SignedCombination> SignedCombinations { get; set; }

            public class PdfModel
            {
                [Required]
                public string PdfId { get; set; }

                [Required]
                public string PdfBytesBase64 { get; set; }

                public string PdfDisplayFileName { get; set; }
            }

            public class SignedCombination
            {
                [Required]
                public string CombinationId { get; set; }

                [Required]
                public List<string> PdfIds { get; set; }

                public string CombinationFileName { get; set; }
            }
        }

        public class StartSignatureSessionRequestBase
        {
            [Required]
            public Dictionary<int, Customer> SigningCustomersByApplicantNr { get; set; }

            [Required]
            public string RedirectAfterSuccessUrl { get; set; }

            [Required]
            public string RedirectAfterFailedUrl { get; set; }

            public string ServerToServerCallbackUrl { get; set; }

            public Dictionary<string, string> CustomData { get; set; }
            public string AlternateSessionKey { get; set; }

            public class Customer
            {
                [Required]
                public string CivicRegNr { get; set; }

                public string FirstName { get; set; }
                public string LastName { get; set; }

                [Required]
                public int ApplicantNr { get; set; }

                public string UserLanguage { get; set; }

                [Required]
                public string SignicatLoginMethod { get; set; }
            }
        }
    }
}