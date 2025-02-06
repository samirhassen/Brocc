using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NTechSignicat.Models;
using NTechSignicat.Services;

namespace NTechSignicat.Controllers
{
    public class SignicatAuthenticationCallbackController : Controller
    {
        private readonly ISignicatAuthenticationService signicatAuthenticationService;

        public SignicatAuthenticationCallbackController(ISignicatAuthenticationService signicatAuthenticationService)
        {
            this.signicatAuthenticationService = signicatAuthenticationService;
        }

        [AllowAnonymous]
        [Route("redirect")]
        public async Task<ActionResult> Callback([FromQuery]AuthenticationRedirectResultModel result)
        {
            if(!string.IsNullOrWhiteSpace(result.error))
            {
                LoginSession session = null;
                if (!string.IsNullOrWhiteSpace(result.state))
                {
                    session = signicatAuthenticationService.ReceiveSignicatErrorCallback(result.state, result.error, result.error_description);
                }

                if(session != null)
                    return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterFailedUrl),
                                Tuple.Create("sessionId", session.Id)).ToString());
                else
                    return Content($"Error: {result.error} - {result.error_description}");
            }
            else if(!string.IsNullOrWhiteSpace(result.code)  && !string.IsNullOrWhiteSpace(result.state))
            {
                var session = await signicatAuthenticationService.ReceiveSignicatSuccessCallback(result.state, result.code);
                if(session == null)
                {
                    return Content($"No such session exists");
                }
                var sessionState = session.GetState();
                if (sessionState != LoginSessionStateCode.PendingLogin)
                    return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterFailedUrl),
                            Tuple.Create("sessionId", session.Id)).ToString());
                else
                    return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterSuccessUrl),
                        Tuple.Create("sessionId", session.Id),
                        Tuple.Create("loginToken", session.OneTimeInternalLoginToken)).ToString());
            }
            else
            {
                return Content($"Missing code and/or state");
            }
        }

        public class AuthenticationRedirectResultModel
        {
            public string error { get; set; }
            public string error_description { get; set; }
            public string code { get; set; }
            public string state { get; set; }
        }
    }
}
