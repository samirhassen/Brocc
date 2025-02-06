using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;

namespace NTech.Services.Infrastructure
{
    public abstract class NHttpServiceBase
    {
        private IHttpClientFactory httpClientFactory;
        private readonly NTechServiceRegistry serviceRegistry;
        private readonly IServiceProvider serviceProvider;

        public abstract string ServiceName { get; }

        public NHttpServiceBase(IHttpClientFactory httpClientFactory, NTechServiceRegistry serviceRegistry, IServiceProvider serviceProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.serviceRegistry = serviceRegistry;
            this.serviceProvider = serviceProvider;
        }

        protected abstract Task<NHttpCall> Begin(TimeSpan? timeout = null);

        protected async Task<T> Call<T>(Func<NHttpCall, Task<NHttpCallResult>> call, Func<NHttpCallResult, Task<T>> handleResult, TimeSpan? timeout = null)
        {
            var c = await Begin(timeout: timeout);
            var r = await call(c);
            return await handleResult(r);
        }

        protected async Task<T> Call<T>(Func<NHttpCall, Task<NHttpCallResult>> call, Func<NHttpCallResult, T> handleResult, TimeSpan? timeout = null)
        {
            var c = await Begin(timeout: timeout);
            var r = await call(c);
            return handleResult(r);
        }

        protected async Task CallVoid(Func<NHttpCall, Task<NHttpCallResult>> call, Func<NHttpCallResult, Task> handleResult, TimeSpan? timeout = null)
        {
            var c = await Begin(timeout: timeout);
            var r = await call(c);
            await handleResult(r);
        }

        protected async Task CallVoid(Func<NHttpCall, Task<NHttpCallResult>> call, Action<NHttpCallResult> handleResult, TimeSpan? timeout = null)
        {
            var c = await Begin(timeout: timeout);
            var r = await call(c);
            handleResult(r);
        }

        public NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(string username, string password)
        {
            return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(this.serviceRegistry, username, password);
        }

        public string GetCurrentAccessToken(System.Security.Claims.ClaimsIdentity overrideUser = null)
        {
            System.Security.Claims.ClaimsIdentity user = overrideUser;
            if (user == null)
            {
                var httpContextAccessor = this.serviceProvider.GetService<IHttpContextAccessor>();

                user = (httpContextAccessor.HttpContext.User.Identity) as System.Security.Claims.ClaimsIdentity;
            }

            string accessToken = null;
            if (user != null)
            {
                accessToken = user?.FindFirst("access_token")?.Value;
            }
            if (accessToken == null)
            {
                var httpContextAccessor = this.serviceProvider.GetService<IHttpContextAccessor>();
                var h = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"];
                if (h.HasValue && h.Value.First().StartsWith("Bearer"))
                {
                    accessToken = h.Value.First().Substring("Bearer".Length).Trim();
                }
            }

            if (accessToken == null)
                throw new Exception("Missing access token");

            return accessToken;
        }

        protected Tuple<Uri, string> SplitUriIntoBaseAndRelative(Uri uri)
        {
            return Tuple.Create(new Uri(uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority)), uri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));
        }

        public class NHttpCall
        {
            private HttpClient client;

            public NHttpCall(HttpClient client)
            {
                this.client = client;
            }

            internal Uri BaseUrl { get; set; }
            internal string BearerToken { get; set; }
            internal TimeSpan? Timeout { get; set; }
            
            private async Task<NHttpCallResult> Call(string relativeUrl, HttpMethod method, Action<HttpRequestMessage> prepare)
            {
                var request = new HttpRequestMessage(method, NTechServiceRegistry.CreateUrl(BaseUrl, relativeUrl));
                if(BearerToken != null)
                {
                    request.Headers.Add("Authorization", $"Bearer {BearerToken}");
                }
                if(method != HttpMethod.Get)
                {
                    request.Headers.Add("X-Ntech-Api-Call", "1");
                }
                prepare(request);
                var response = await client.SendAsync(request);
                return new NHttpCallResult(response);
            }
            
            private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
            {
                var s = new JsonSerializerSettings();
                s.NullValueHandling = NullValueHandling.Ignore;
                return s;
            });

            private static string SerializeObject<T>(T value, bool allowSkipNulls)
            {
                return allowSkipNulls ? JsonConvert.SerializeObject(value, NullIgnoringJsonSerializerSettings.Value) : JsonConvert.SerializeObject(value);
            }

            public async Task<NHttpCallResult> PostJson<T>(string relativeUrl, T value, bool allowSkipNulls = false)
            {
                return await PostJsonRaw(relativeUrl, SerializeObject(value, allowSkipNulls));
            }

            public async Task<NHttpCallResult> PostJsonRaw(string relativeUrl, string json)
            {
                return await Call(relativeUrl, HttpMethod.Post, request =>
                {
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                });
            }

            public async Task<NHttpCallResult> UploadFile(string relativeUrl, Stream file, string uploadFilename, string mimeType)
            {
                return await Call(relativeUrl, HttpMethod.Post, request =>
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

            public async Task<NHttpCallResult> Get(string relativeUrl)
            {
                return await Call(relativeUrl, HttpMethod.Get, request => { });
            }
        }
        public class NHttpCallResult
        {
            private readonly HttpResponseMessage response;

            public NHttpCallResult(HttpResponseMessage response)
            {
                this.response = response;                
            }

            public bool IsSuccessStatusCode
            {
                get
                {
                    return response.IsSuccessStatusCode;
                }
            }

            public bool IsNotFoundStatusCode
            {
                get
                {
                    return response.StatusCode == System.Net.HttpStatusCode.NotFound;
                }
            }

            public int StatusCode
            {
                get
                {
                    return (int)response.StatusCode;
                }
            }

            public string ReasonPhrase
            {
                get
                {
                    return response.ReasonPhrase;
                }
            }

            public void EnsureSuccessStatusCode()
            {
                response.EnsureSuccessStatusCode();
            }

            public bool IsApiError
            {
                get
                {
                    IEnumerable<string> v;
                    return response.Headers.TryGetValues("X-Ntech-Api-Error", out v) && v.FirstOrDefault() == "1";
                }
            }

            public async Task<ApiError> ParseApiError()
            {
                return JsonConvert.DeserializeObject<ApiError>(await response.Content.ReadAsStringAsync());
            }

            public async Task<T> HandlingApiError<T>(Func<NHttpCallResult, T> success, Func<ApiError, T> error)
            {
                if (IsApiError)
                    return error(await ParseApiError());
                else
                    return success(this);
            }

            public class ApiError
            {
                protected string ErrorMessage { get; set; }
                protected string ErrorCode { get; set; }
            }

            public async Task<string> ParseAsRawJson(bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                    throw new Exception($"Expected application/json but instead got: {response.Content.Headers.ContentType.ToString()}");
                return await response.Content.ReadAsStringAsync();
            }

            public async Task<T> ParseJsonAs<T>(bool allowNonSucessStatusCode = false)
            {
                if(!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                    throw new Exception($"Expected application/json but instead got: {response.Content.Headers.ContentType.ToString()}");
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            }

            public async Task<T> ParseJsonAsAnonymousType<T>(T anonymousTypeObject, bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                    throw new Exception($"Expected application/json but instead got: {response.Content.Headers.ContentType.ToString()}");
                return JsonConvert.DeserializeAnonymousType<T>(await response.Content.ReadAsStringAsync(), anonymousTypeObject);
            }

            public async Task CopyToStream(Stream target)
            {
                EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType.Contains("html"))
                    throw new Exception($"Expected a binary response but instead got: {response.Content.Headers.ContentType.ToString()}");
                await response.Content.CopyToAsync(target);
            }

            public async Task<Tuple<string, string>> DownloadFile(Stream target, bool allowHtml = false)
            {
                EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType.Contains("html") && !allowHtml)
                    throw new Exception($"Expected a binary response but instead got: {response.Content.Headers.ContentType.ToString()}");
                               
                var contentType = response.Content.Headers.ContentType.MediaType;
                var filename = response.Content.Headers.ContentDisposition?.FileName;

                await response.Content.CopyToAsync(target);

                return Tuple.Create(contentType, filename);
            }
        }

        protected NHttpCall Begin(string bearerToken, TimeSpan? timeout = null)
        {
            return new NHttpCall(this.httpClientFactory.CreateClient())
            {
                BaseUrl = this.serviceRegistry.Internal.ServiceRootUri(ServiceName),
                BearerToken = bearerToken,
                Timeout = timeout
            };
        }
    }
}