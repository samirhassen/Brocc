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
    public class TestLoginController : Controller
    {
        private readonly ISignicatAuthenticationService signicatAuthenticationService;
        private readonly SignicatSettings signicatSettings;
        private readonly SignicatLoginMethodValidator signicatLoginMethodValidator;

        public TestLoginController(ISignicatAuthenticationService signicatAuthenticationService, SignicatSettings signicatSettings, SignicatLoginMethodValidator signicatLoginMethodValidator)
        {
            this.signicatAuthenticationService = signicatAuthenticationService;
            this.signicatSettings = signicatSettings;
            this.signicatLoginMethodValidator = signicatLoginMethodValidator;
        }

        [AllowAnonymous]
        [Route("test-login")]
        public async Task<IActionResult> Login([FromQuery]TestLoginRequest request)
        {
            var supportedLoginMethods = string.Join(", ", Enum.GetNames(typeof(SignicatLoginMethodCode)).Where(x => x != SignicatLoginMethodCode.Invalid.ToString()));

            if (string.IsNullOrWhiteSpace(request?.CivicRegNr))
            {                
                return Content($"Allowed parameters: loginMethod ({supportedLoginMethods}), civicRegNr");
            }
            SignicatLoginMethodValidator.ValidatedParameters validatedParameters;
            if (!signicatLoginMethodValidator.TryValidate(request?.CivicRegNr, request?.LoginMethod == null ? null : new List<string> { request.LoginMethod }, out validatedParameters))
                return Ok($"Invalid parameters. Requires loginMethod and civicRegNr. Supported login methods are: {{supportedLoginMethods}}");

            var session = await signicatAuthenticationService.StartLoginSession(
                validatedParameters.ExpectedCivicRegNr,
                validatedParameters.LoginMethods,
                UrlBuilder.Create(signicatSettings.SelfExternalUrl, "test-login-ok").ToUri(),
                UrlBuilder.Create(signicatSettings.SelfExternalUrl, "test-login-failed").ToUri());
            return Redirect(session.SignicatInitialUrl);
        }

        [AllowAnonymous]
        [Route("test-login-ok")]
        public IActionResult LoginOk([FromQuery]LoginOkRequest request)
        {
            var session = this.signicatAuthenticationService.CompleteInternalLogin(request.SessionId, request.LoginToken);
            if (session == null)
                return Ok("No such session");

            var state = session.GetState();
            if(state != LoginSessionStateCode.LoginSuccessful)
                return Ok("Login failed");

            var user = session.UserInfo;

            return Ok($"Logged in as: {user.FirstName} - {user.LastName} - {user.CivicRegNr}");
        }

        [AllowAnonymous]
        [Route("test-login-failed")]
        public IActionResult LoginFailed([FromQuery]string sessionId)
        {
            var session = this.signicatAuthenticationService.GetLoginSession(sessionId);
            return Ok($"Login failed: {session.FailedCode} - {session.FailedMessage}");
        }

        public class TestLoginRequest
        {
            public string CivicRegNr { get; set; }
            public string LoginMethod { get; set; }
        }

        public class LoginOkRequest
        {
            public string SessionId { get; set; }
            public string LoginToken { get; set; }
        }
    }
}
