using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Module.Shared.Clients
{
    public class ServiceClient : IServiceClientSyncConverter
    {
        private const string CoreHostServiceName = "NTechHost";
        private const string JsonMimeType = "application/json";
        private readonly INHttpServiceUser httpServiceUser;
        private readonly Func<string, Uri> getServiceRootUri;
        private readonly Func<HttpClient> createHttpClient;
        private readonly IServiceClientSyncConverter serviceClientSyncConverter;
        private readonly string serviceName;

        public ServiceClient(INHttpServiceUser httpServiceUser, Func<string, Uri> getServiceRootUri,
            Func<HttpClient> createHttpClient, IServiceClientSyncConverter serviceClientSyncConverter,
            string serviceName)
        {
            this.httpServiceUser = httpServiceUser;
            this.getServiceRootUri = getServiceRootUri;
            this.createHttpClient = createHttpClient;
            this.serviceClientSyncConverter = serviceClientSyncConverter;
            this.serviceName = serviceName;
        }

        private async Task<NHttpCall> BeginAsync(TimeSpan? timeout = null, bool isCoreHosted = false)
        {
            return new NHttpCall(createHttpClient())
            {
                BaseUrl = getServiceRootUri(isCoreHosted ? CoreHostServiceName : serviceName),
                BearerToken = await httpServiceUser.GetBearerTokenAsync(),
                Timeout = timeout
            };
        }

        public async Task<T> Call<T>(Func<NHttpCall, Task<NHttpCallResult>> call,
            Func<NHttpCallResult, Task<T>> handleResult,
            TimeSpan? timeout = null, bool isCoreHosted = false)
        {
            var c = await BeginAsync(timeout: timeout, isCoreHosted: isCoreHosted);
            var r = await call(c);
            var result = await handleResult(r);
            return result;
        }

        public async Task<T> Call<T>(Func<NHttpCall, Task<NHttpCallResult>> call, Func<NHttpCallResult, T> handleResult,
            TimeSpan? timeout = null, bool isCoreHosted = false)
        {
            var c = await BeginAsync(timeout: timeout, isCoreHosted: isCoreHosted);
            var r = await call(c);
            return handleResult(r);
        }

        public async Task CallVoid(Func<NHttpCall, Task<NHttpCallResult>> call,
            Func<NHttpCallResult, Task> handleResult, TimeSpan? timeout = null, bool isCoreHosted = false)
        {
            var c = await BeginAsync(timeout: timeout, isCoreHosted: isCoreHosted);
            var r = await call(c);
            await handleResult(r);
        }

        public async Task CallVoid(Func<NHttpCall, Task<NHttpCallResult>> call, Action<NHttpCallResult> handleResult,
            TimeSpan? timeout = null, bool isCoreHosted = false)
        {
            var c = await BeginAsync(timeout: timeout, isCoreHosted: isCoreHosted);
            var r = await call(c);
            handleResult(r);
        }

        public TResult ToSync<TResult>(Func<Task<TResult>> action) => serviceClientSyncConverter.ToSync(action);

        public void ToSync(Func<Task> action) => serviceClientSyncConverter.ToSync(action);

        public class NHttpCall
        {
            private readonly HttpClient client;

            public NHttpCall(HttpClient client)
            {
                this.client = client;
            }

            internal Uri BaseUrl { get; set; }
            internal string BearerToken { get; set; }
            internal TimeSpan? Timeout { get; set; }

            private async Task<NHttpCallResult> Call(string relativeUrl, HttpMethod method,
                Action<HttpRequestMessage> prepare)
            {
                var request = new HttpRequestMessage(method, CreateUrl(BaseUrl, relativeUrl));
                if (BearerToken != null)
                {
                    request.Headers.Add("Authorization", $"Bearer {BearerToken}");
                }

                if (method != HttpMethod.Get)
                {
                    request.Headers.Add("X-Ntech-Api-Call", "1");
                }

                prepare(request);
                var response = await client.SendAsync(request);
                return new NHttpCallResult(response);
            }

            private static string NormalizePath(string path)
            {
                return path?.TrimStart('/').TrimEnd('/') ?? "";
            }

            private static Uri CreateUrl(Uri rootUrl, string relativeUrl,
                params Tuple<string, string>[] queryStringParameters)
            {
                var relativeUrlTrimmed = NormalizePath(relativeUrl);

                var queryParams = new List<Tuple<string, string>>();

                var i = relativeUrlTrimmed.IndexOf('?');
                if (i >= 0)
                {
                    var queryString = relativeUrlTrimmed.Substring(i);
                    var queryStringParsed = HttpUtility.ParseQueryString(queryString);
                    foreach (var p in queryStringParsed.AllKeys)
                        queryParams.Add(Tuple.Create(p, queryStringParsed[p]));
                    relativeUrlTrimmed = relativeUrlTrimmed.Substring(0, i);
                }

                queryParams.AddRange(queryStringParameters.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2)));

                var u = new UriBuilder(
                    rootUrl.Scheme,
                    rootUrl.Host,
                    rootUrl.Port,
                    rootUrl.Segments.Length == 1
                        ? relativeUrlTrimmed
                        : $"{NormalizePath(rootUrl.LocalPath)}/{relativeUrlTrimmed}");

                var query = HttpUtility.ParseQueryString(rootUrl.Query);
                foreach (var p in queryParams)
                    query[p.Item1] = p.Item2;
                u.Query = query.ToString();

                return u.Uri;
            }

            private static readonly Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings =
                new Lazy<JsonSerializerSettings>(() => new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            private static string SerializeObject<T>(T value, bool allowSkipNulls)
            {
                return allowSkipNulls
                    ? JsonConvert.SerializeObject(value, NullIgnoringJsonSerializerSettings.Value)
                    : JsonConvert.SerializeObject(value);
            }

            public async Task<NHttpCallResult> PostJson<T>(string relativeUrl, T value, bool allowSkipNulls = false)
            {
                return await PostJsonRaw(relativeUrl, SerializeObject(value, allowSkipNulls));
            }

            private async Task<NHttpCallResult> PostJsonRaw(string relativeUrl, string json)
            {
                return await Call(relativeUrl, HttpMethod.Post,
                    request => { request.Content = new StringContent(json, Encoding.UTF8, JsonMimeType); });
            }

            public async Task<NHttpCallResult> UploadFile(string relativeUrl, Stream file, string uploadFilename,
                string mimeType)
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

            public bool IsSuccessStatusCode => response.IsSuccessStatusCode;

            public bool IsNotFoundStatusCode => response.StatusCode == HttpStatusCode.NotFound;

            public int StatusCode => (int)response.StatusCode;

            public string ReasonPhrase => response.ReasonPhrase;

            public void EnsureSuccessStatusCode()
            {
                response.EnsureSuccessStatusCode();
            }

            public bool IsApiError =>
                response.Headers.TryGetValues("X-Ntech-Api-Error", out var v) && v.FirstOrDefault() == "1";

            public async Task<ApiError> ParseApiError()
            {
                return JsonConvert.DeserializeObject<ApiError>(await response.Content.ReadAsStringAsync());
            }

            public class ApiError
            {
                public string ErrorMessage { get; set; }
                public string ErrorCode { get; set; }
            }

            public async Task<string> ParseAsRawJson(bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return null;
                if (!response.Content.Headers.ContentType.MediaType.Contains(JsonMimeType))
                    throw new Exception(
                        $"Expected application/json but instead got: {response.Content.Headers.ContentType}");
                return await response.Content.ReadAsStringAsync();
            }

            public async Task<T> ParseJsonAs<T>(bool allowNonSucessStatusCode = false)
            {
                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return default(T);
                if (!response.Content.Headers.ContentType.MediaType.Contains(JsonMimeType))
                    throw new Exception(
                        $"Expected application/json but instead got: {response.Content.Headers.ContentType}");
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            }

            public async Task<T> ParseJsonAsAnonymousType<T>(T anonymousTypeObject,
                bool allowNonSucessStatusCode = false, bool propagateApiError = false)
            {
                if (propagateApiError && IsApiError)
                {
                    var apiError = await ParseApiError();
                    throw new NTechCoreWebserviceException(apiError.ErrorMessage) { ErrorCode = apiError.ErrorCode };
                }

                if (!allowNonSucessStatusCode)
                    EnsureSuccessStatusCode();
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return default(T);
                if (!response.Content.Headers.ContentType.MediaType.Contains(JsonMimeType))
                    throw new Exception(
                        $"Expected application/json but instead got: {response.Content.Headers.ContentType}");
                return JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(),
                    anonymousTypeObject);
            }

            public async Task CopyToStream(Stream target)
            {
                EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType.Contains("html"))
                    throw new Exception(
                        $"Expected a binary response but instead got: {response.Content.Headers.ContentType}");
                await response.Content.CopyToAsync(target);
            }

            public async Task<Tuple<string, string>> DownloadFile(Stream target, bool allowHtml = false)
            {
                EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType.Contains("html") && !allowHtml)
                    throw new Exception(
                        $"Expected a binary response but instead got: {response.Content.Headers.ContentType}");

                var contentType = response.Content.Headers.ContentType.MediaType;
                var filename = response.Content.Headers.ContentDisposition?.FileName;

                await response.Content.CopyToAsync(target);

                return Tuple.Create(contentType, filename);
            }
        }
    }

    public interface IServiceClientSyncConverter
    {
        TResult ToSync<TResult>(Func<Task<TResult>> action);
        void ToSync(Func<Task> action);
    }
}