using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTechSignicat.Models;
using NTechSignicat.Services;
using NTechSignicat.Shared;

namespace NTechSignicat.Controllers
{
    public class TestSignatureController : Controller
    {
        private readonly SignicatSettings signicatSettings;
        private readonly SignicatLoginMethodValidator signicatLoginMethodValidator;
        private readonly ISignicatSignatureService signicatSignatureService;
        private readonly IDocumentService documentService;

        public TestSignatureController(SignicatSettings signicatSettings, SignicatLoginMethodValidator signicatLoginMethodValidator, ISignicatSignatureService signicatSignatureService, IDocumentService documentService)
        {
            this.signicatSettings = signicatSettings;
            this.signicatLoginMethodValidator = signicatLoginMethodValidator;
            this.signicatSignatureService = signicatSignatureService;
            this.documentService = documentService;
        }

        [AllowAnonymous]
        [Route("test-signature")]
        public async Task<IActionResult> TestSignature([FromQuery] TestSignatureRequest request)
        {
            var supportedLoginMethods = string.Join(", ", Enum.GetNames(typeof(SignicatLoginMethodCode)).Where(x => x != SignicatLoginMethodCode.Invalid.ToString()));

            if (string.IsNullOrWhiteSpace(request?.CivicRegNr1))
            {
                return Content($"Allowed parameters: loginMethod ({supportedLoginMethods}), civicRegNr1, [firstName1], [lastName1], [userLanguage1], civicRegNr2, [firstName2], [lastName2], [userLanguage2]");
            }
            SignicatLoginMethodValidator.ValidatedParameters validatedParameters;
            if (!signicatLoginMethodValidator.TryValidate(request?.CivicRegNr1, request?.LoginMethod == null ? null : new List<string> { request.LoginMethod }, out validatedParameters))
                return Ok("Invalid parameters. Requires loginMethod and civicRegNr1.  Supported login methods are: " + supportedLoginMethods);

            SignicatLoginMethodValidator.ValidatedParameters validatedParameters2 = null;
            if (!string.IsNullOrWhiteSpace(request.CivicRegNr2))
            {
                if (!signicatLoginMethodValidator.TryValidate(request?.CivicRegNr2, request?.LoginMethod == null ? null : new List<string> { request.LoginMethod }, out validatedParameters2))
                    return Ok("Invalid parameters. Requires loginMethod and civicRegNr2.  Supported login methods are: " + supportedLoginMethods);
            }

            byte[] contentBytes;
            var testPdfPath = signicatSettings.SignatureTestPdfPath;
            if (testPdfPath == null)
            {
                contentBytes = Pdfs.CreateMinimalPdf($"{DateTime.Now.ToString("HH:mm")}: Test sign {request.CivicRegNr1}");
            }
            else
            {
                contentBytes = System.IO.File.ReadAllBytes(testPdfPath);
            }

            var customers = new Dictionary<int, SignatureRequestCustomer>();
            customers[1] = new SignatureRequestCustomer
            {
                ApplicantNr = 1,
                CivicRegNr = validatedParameters.ExpectedCivicRegNr,
                FirstName = request.FirstName1 ?? "TestFirstname1",
                LastName = request.LastName1 ?? "TestLastname1",
                SignicatLoginMethod = validatedParameters.LoginMethods.Single(),
                UserLanguage = request.UserLanguage1 ?? (validatedParameters.LoginMethods.Single() == SignicatLoginMethodCode.FinnishTrustNetwork ? "fi" : "sv")
            };
            if (validatedParameters2 != null)
            {
                customers[2] = new SignatureRequestCustomer
                {
                    ApplicantNr = 2,
                    CivicRegNr = validatedParameters2.ExpectedCivicRegNr,
                    FirstName = request.FirstName2 ?? "TestFirstname2",
                    LastName = request.LastName2 ?? "TestLastname2",
                    SignicatLoginMethod = validatedParameters2.LoginMethods.Single(),
                    UserLanguage = request.UserLanguage2 ?? (validatedParameters2.LoginMethods.Single() == SignicatLoginMethodCode.FinnishTrustNetwork ? "fi" : "sv")
                };
            }

            var session = await this.signicatSignatureService.CreatePdfSignatureRequest(
                customers,
                contentBytes, "test-signature-document.pdf",
                UrlBuilder.Create(signicatSettings.SelfExternalUrl, "test-signature-ok").ToUri(),
                UrlBuilder.Create(signicatSettings.SelfExternalUrl, "test-signature-failed").ToUri());

            return Redirect(session.GetNextSignatureUrl());
        }

        [AllowAnonymous]
        [Route("test-signature-ok")]
        public IActionResult SignatureOk([FromQuery] string sessionId)
        {
            var session = this.signicatSignatureService.GetSession(sessionId);
            if (session == null)
                return Ok("No such session");

            var state = session.GetState();
            if (session.SigningCustomersByApplicantNr.Count > 1 && state == SignatureSessionStateCode.PendingSomeSignatures)
                return Redirect(session.GetNextSignatureUrl());

            if (state != SignatureSessionStateCode.SignaturesSuccessful)
                return Ok("Signature failed");

            var documentBytes = documentService.Get(session.SignedDocumentKey)?.GetDocumentData();
            return File(documentBytes, "application/pdf"); //Actually something called application/x-pades
        }

        [AllowAnonymous]
        [Route("test-signature-failed")]
        public IActionResult SignatureFailed([FromQuery] string sessionId)
        {
            var session = this.signicatSignatureService.GetSession(sessionId);
            if (session == null)
                return Ok("No such session");
            return Ok($"Signature failed: {session.SessionStateCode} - {session.SessionStateMessage}");
        }
    }

    public class TestSignatureRequest
    {
        public string CivicRegNr1 { get; set; }
        public string FirstName1 { get; set; }
        public string LastName1 { get; set; }
        public string UserLanguage1 { get; set; }
        public string CivicRegNr2 { get; set; }
        public string FirstName2 { get; set; }
        public string LastName2 { get; set; }
        public string UserLanguage2 { get; set; }
        public string LoginMethod { get; set; }
    }
}