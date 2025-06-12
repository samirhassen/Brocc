using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Web;
using IdentityModel.Client;
using Newtonsoft.Json;

namespace NTech.Services.Infrastructure
{
    public static class NHttp
    {
        private static readonly Dictionary<int, HttpClient>
            ClientByTimeout =
                new Dictionary<int, HttpClient>(); //That you cant just set the timeout on each request is beyond insane

        private static readonly object ClientPoolLock = new object();

        private static HttpClient GetClient(TimeSpan? timeout)
        {
            var ts = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1;
            if (ClientByTimeout.TryGetValue(ts, out var client))
                return client;

            lock (ClientPoolLock)
            {
                if (ClientByTimeout.TryGetValue(ts, out var client1))
                    return client1;

                var c = new HttpClient();
                c.DefaultRequestHeaders.Clear();
                if (timeout.HasValue)
                    c.Timeout = timeout.Value;
                ClientByTimeout.Add(ts, c);

                return ClientByTimeout[ts];
            }
        }

        public static string AquireSystemUserAccessTokenWithUsernamePassword(string username, string password,
            Uri userServiceUrl)
        {
            var tokenClient = new TokenClient(
                NTechServiceRegistry.CreateUrl(userServiceUrl, "id/connect/token").ToString(),
                "nTechSystemUser",
                "nTechSystemUser");

            var token = tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: "nTech1").Result;

            if (token.IsError)
            {
                throw new Exception(token.Error);
            }
            else
            {
                return token.AccessToken;
            }
        }

        public static string GetCurrentAccessToken(ClaimsIdentity overrideUser = null)
        {
            string accessToken = null;
            var user = (overrideUser ?? (HttpContext.Current?.User?.Identity)) as ClaimsIdentity;

            if (user != null)
            {
                accessToken = user?.FindFirst("access_token")?.Value;
            }

            if (accessToken == null)
            {
                var h = HttpContext.Current?.Request?.Headers["Authorization"];
                if (h != null && h.StartsWith("Bearer"))
                {
                    accessToken = h.Substring("Bearer".Length).Trim();
                }
            }

            if (accessToken == null)
                throw new Exception("Missing access token");

            return accessToken;
        }

        public static Tuple<Uri, string> SplitUriIntoBaseAndRelative(Uri uri)
        {
            return Tuple.Create(new Uri(uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority)),
                uri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));
        }

        public class NHttpCall
        {
            internal Uri BaseUrl { get; set; }
            internal string BearerToken { get; set; }
            internal TimeSpan? Timeout { get; set; }

            private NHttpCallResult Call(string relativeUrl, HttpMethod method, Action<HttpRequestMessage> prepare)
            {
                var request = new HttpRequestMessage(method, NTechServiceRegistry.CreateUrl(BaseUrl, relativeUrl));
                if (BearerToken != null)
                {
                    request.Headers.Add("Authorization", $"Bearer {BearerToken}");
                }

                if (method != HttpMethod.Get)
                {
                    request.Headers.Add("X-Ntech-Api-Call", "1");
                }

                prepare(request);
                var response = GetClient(Timeout).SendAsync(request);

                return new NHttpCallResult(() => response.Result);
            }

            private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings =
                new Lazy<JsonSerializerSettings>(() =>
                {
                    return new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };
                });

            private static string SerializeObject<T>(T value, bool allowSkipNulls)
            {
                return allowSkipNulls
                    ? JsonConvert.SerializeObject(value, NullIgnoringJsonSerializerSettings.Value)
                    : JsonConvert.SerializeObject(value);
            }

            public NHttpCallResult PostJson<T>(string relativeUrl, T value, bool allowSkipNulls = false,
                Dictionary<string, string> headers = null)
            {
                return PostJsonRaw(relativeUrl, SerializeObject(value, allowSkipNulls), headers: headers);
            }

            public NHttpCallResult PostJsonRaw(string relativeUrl, string json,
                Dictionary<string, string> headers = null)
            {
                return Call(relativeUrl, HttpMethod.Post, request =>
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    if (headers == null) return;
                    foreach (var h in headers)
                        request.Headers.Add(h.Key, h.Value);
                });
            }

            public NHttpCallResult UploadFile(string relativeUrl, Stream file, string uploadFilename, string mimeType)
            {
                return Call(relativeUrl, HttpMethod.Post, request =>
                {
                    var requestContent = new MultipartFormDataContent();
                    var fileContent = new StreamContent(file);
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        Name = "\"file\"",
                        FileName = "\"" + uploadFilename + "\""
                    };
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                    requestContent.Add(fileContent);
                    request.Content = requestContent;
                });
            }

            public NHttpCallResult Get(string relativeUrl)
            {
                return Call(relativeUrl, HttpMethod.Get, request => { });
            }
        }

        public class NHttpCallResult
        {
            private readonly HttpResponseMessage response;

            public NHttpCallResult(HttpResponseMessage response)
            {
                this.response = response;
            }

            public NHttpCallResult(Func<HttpResponseMessage> receiveResponse)
            {
                try
                {
                    this.response = receiveResponse();
                }
                catch (Exception ex)
                {
                    var se = FindInnerExceptionOfType<SocketException>(ex);
                    if (se != null)
                        throw new Exception($"The service seems to be down: {se.Message}", se);
                    throw;
                }
            }

            private static T FindInnerExceptionOfType<T>(Exception ex) where T : Exception
            {
                var e = ex;
                var i = 0;
                while (e != null && i++ < 100)
                {
                    var se = e as T;
                    if (se != null)
                        return se;
                    e = e.InnerException;
                }

                return null;
            }

            public string ContentType => response.Content.Headers.ContentType.ToString();

            public bool IsSuccessStatusCode => response.IsSuccessStatusCode;

            public bool IsNotFoundStatusCode => response.StatusCode == HttpStatusCode.NotFound;

            public int StatusCode => (int)response.StatusCode;

            public string ReasonPhrase => response.ReasonPhrase;

            public void EnsureSuccessStatusCode()
            {
                response.EnsureSuccessStatusCode();
            }

            public bool IsApiError => response.Headers.TryGetValues("X-Ntech-Api-Error", out var v) &&
                                      v.FirstOrDefault() == "1";

            public ApiError ParseApiError()
            {
                return JsonConvert.DeserializeObject<ApiError>(response.Content.ReadAsStringAsync().Result);
            }

            public T HandlingApiError<T>(Func<NHttpCallResult, T> success, Func<ApiError, T> error)
            {
                return IsApiError ? error(ParseApiError()) : success(this);
            }

            public T HandlingApiErrorWithHttpCode<T>(Func<NHttpCallResult, T> success,
                Func<ApiError, HttpStatusCode, T> error)
            {
                return IsApiError ? error(ParseApiError(), response.StatusCode) : success(this);
            }

            public class ApiError
            {
                public string ErrorMessage { get; set; }
                public string ErrorCode { get; set; }
            }

            public string ParseAsRawJson(bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                    throw new Exception(
                        $"Expected application/json but instead got: {response.Content.Headers.ContentType.ToString()}");
                return response.Content.ReadAsStringAsync().Result;
            }

            public T ParseJsonAs<T>(bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                    throw new Exception(
                        $"Expected application/json but instead got: {response.Content.Headers.ContentType.ToString()}");
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            public T ParseJsonAsAnonymousType<T>(T anonymousTypeObject, bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                    throw new Exception(
                        $"Expected application/json but instead got: {response.Content.Headers.ContentType.ToString()}");
                return JsonConvert.DeserializeAnonymousType<T>(response.Content.ReadAsStringAsync().Result,
                    anonymousTypeObject);
            }

            public void CopyToStream(Stream target)
            {
                EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType.Contains("html"))
                    throw new Exception(
                        $"Expected a binary response but instead got: {response.Content.Headers.ContentType.ToString()}");
                response.Content.CopyToAsync(target).Wait();
            }

            public void DownloadFile(Stream target, out string contentType, out string filename, bool allowHtml = false)
            {
                EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType.Contains("html") && !allowHtml)
                    throw new Exception(
                        $"Expected a binary response but instead got: {response.Content.Headers.ContentType.ToString()}");

                contentType = response.Content.Headers.ContentType.MediaType;
                filename = response.Content.Headers.ContentDisposition?.FileName;

                filename = filename?.Replace("\"", "");

                response.Content.CopyToAsync(target).Wait();
            }
        }

        public static NHttpCall Begin(Uri baseUrl, string bearerToken, TimeSpan? timeout = null)
        {
            return new NHttpCall
            {
                BaseUrl = baseUrl,
                BearerToken = bearerToken,
                Timeout = timeout
            };
        }
    }
}