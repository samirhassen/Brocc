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
    public class AuthenticationApiController : Controller
    {
        private readonly ISignicatAuthenticationService signicatAuthenticationService;
        private readonly SignicatLoginMethodValidator signicatLoginMethodValidator;

        public AuthenticationApiController(ISignicatAuthenticationService signicatAuthenticationService, SignicatLoginMethodValidator signicatLoginMethodValidator)
        {
            this.signicatAuthenticationService = signicatAuthenticationService;
            this.signicatLoginMethodValidator = signicatLoginMethodValidator;
        }

        [Route("get-login-session")]
        [HttpPost]
        public IActionResult GetLoginSession([FromBody]GetLoginSessionRequest request)
        {
            return Json(this.signicatAuthenticationService.GetLoginSession(request?.SessionId));
        }

        [Route("start-login-session")]
        [HttpPost]
        public async Task<IActionResult> StartLoginSession([FromBody]StartLoginSessionRequest request)
        {
            SignicatLoginMethodValidator.ValidatedParameters validatedParameters;
            if (!signicatLoginMethodValidator.TryValidate(request?.ExpectedCivicRegNr, request?.LoginMethods, out validatedParameters))
                return BadRequest();

            return Json(
                await this.signicatAuthenticationService.StartLoginSession(
                    validatedParameters.ExpectedCivicRegNr,
                    validatedParameters.LoginMethods,
                    new Uri(request.RedirectAfterSuccessUrl),
                    new Uri(request.RedirectAfterFailedUrl),
                     customData: request.CustomData));
        }

        [Route("complete-login-session")]
        [HttpPost]
        public IActionResult CompleteLoginSession([FromBody]CompleteLoginSessionRequest request)
        {
            return Json(this.signicatAuthenticationService.CompleteInternalLogin(request?.SessionId, request?.Token));
        }

        public class CompleteLoginSessionRequest
        {
            [Required]
            public string SessionId { get; set; }
            [Required]
            public string Token { get; set; }
        }

        public class GetLoginSessionRequest
        {
            [Required]
            public string SessionId { get; set; }
        }

        public class StartLoginSessionRequest
        {
            public string ExpectedCivicRegNr { get; set; }

            [Required]
            public List<string> LoginMethods { get; set; }

            [Required]
            public string RedirectAfterSuccessUrl { get; set; }

            [Required]
            public string RedirectAfterFailedUrl { get; set; }

            public Dictionary<string, string> CustomData { get; set; }
        }
    }
}
