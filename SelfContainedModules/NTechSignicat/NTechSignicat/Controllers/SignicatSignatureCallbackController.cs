using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NTechSignicat.Models;
using NTechSignicat.Services;

namespace NTechSignicat.Controllers
{
    public class SignicatSignatureCallbackController : Controller
    {
        private readonly ISignicatSignatureService signatureService;
        private readonly IHttpClientFactory httpClientFactory;

        public SignicatSignatureCallbackController(ISignicatSignatureService signatureService, IHttpClientFactory httpClientFactory)
        {
            this.signatureService = signatureService;
            this.httpClientFactory = httpClientFactory;
        }

        [AllowAnonymous]
        [Route("signature-redirect")]
        public async Task<ActionResult> Callback([FromQuery]SignatureRedirectResultModel result)
        {
            if(string.IsNullOrWhiteSpace(result?.request_id) || string.IsNullOrWhiteSpace(result?.status) || string.IsNullOrWhiteSpace(result?.taskId))
                return Content("Missing request_id, taskId or status");

            var session = await this.signatureService.HandleSignatureCallback(result.request_id, result.taskId, result.status);

            if(session == null)
                return Content("No such session");

            var state = session.GetState();

            switch(state)
            {
                case SignatureSessionStateCode.Broken:
                case SignatureSessionStateCode.Cancelled:
                    return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterFailedUrl),
                            Tuple.Create("sessionId", session.Id),
                            Tuple.Create("isCancelled", state == SignatureSessionStateCode.Cancelled ? "true" : "false")).ToString());
                case SignatureSessionStateCode.Failed:
                    {
                        if (session.ServerToServerCallbackUrl != null)
                        {
                            await Postback(session.ServerToServerCallbackUrl, new
                            {
                                sessionId = session.Id,
                                providerName = "signicat",
                                eventName = "Failure",
                                errorMessage = session.SessionStateMessage
                            });
                        }
                        return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterFailedUrl),
                                Tuple.Create("sessionId", session.Id)).ToString());
                    }
                case SignatureSessionStateCode.PendingAllSignatures:
                    return Redirect(session.GetNextSignatureUrl());
                case SignatureSessionStateCode.PendingSomeSignatures:
                    {
                        if (session.ServerToServerCallbackUrl != null)
                        {
                            await Postback(session.ServerToServerCallbackUrl, new
                            {
                                sessionId = session.Id,
                                providerName = "signicat",
                                eventName = "Intermediate",
                            });
                        }
                        return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterSuccessUrl),
                            Tuple.Create("sessionId", session.Id)).ToString());
                    }
                case SignatureSessionStateCode.SignaturesSuccessful:
                    {
                        if(session.ServerToServerCallbackUrl != null)
                        {
                            await Postback(session.ServerToServerCallbackUrl, new
                            {
                                sessionId = session.Id,
                                providerName = "signicat",
                                eventName = "Success",
                            });
                        }
                        return Redirect(UrlBuilder.AppendQueryStringParams(new Uri(session.RedirectAfterSuccessUrl),
                            Tuple.Create("sessionId", session.Id)).ToString());
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task<bool> Postback<T>(string url, T data)
        {
            var client = httpClientFactory.CreateClient();
            var result = await client.PostAsJsonAsync(url, data);
            return result.IsSuccessStatusCode;
        }

        private static Tuple<Uri, string> SplitUriIntoBaseAndRelative(Uri uri)
        {
            return Tuple.Create(new Uri(uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority)), uri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));
        }

        public class SignatureRedirectResultModel
        {
            public string request_id { get; set; }
            public string taskId { get; set; }
            public string status { get; set; }
        }
    }
}
