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

namespace NTechSignicat.Controllers
{
    public class SimpleAfterSignatureLandingPageController : Controller
    {
        private readonly ISignicatSignatureService signicatSignatureService;

        public SimpleAfterSignatureLandingPageController(ISignicatSignatureService signicatSignatureService)
        {
            this.signicatSignatureService = signicatSignatureService;
        }

        [AllowAnonymous]
        [Route("ui/s/signature-ok")]
        public IActionResult SignatureOk([FromQuery]string sessionId)
        {
            var session = this.signicatSignatureService.GetSession(sessionId);
            if (session == null)
                return Ok("No such session");

            var state = session.GetState();

            if(state == SignatureSessionStateCode.PendingSomeSignatures)
                return Ok("Signature successful but still pending other signatories.");

            if (state != SignatureSessionStateCode.SignaturesSuccessful)
                return Ok("Signature failed");

            return Ok("Thank you for your signature.");
        }

        [AllowAnonymous]
        [Route("ui/s/signature-failed")]
        public IActionResult SignatureFailed([FromQuery]string sessionId)
        {
            var session = this.signicatSignatureService.GetSession(sessionId);
            if (session == null)
                return Ok("No such session");
            return Ok($"Signature failed: {session.SessionStateMessage}");
        }
    }
}