using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace nPreCredit.Code.AffiliateReporting
{
    public abstract class JsonAffiliateWebserviceBase
    {
        protected Lazy<HttpClient> httpClient = new Lazy<HttpClient>(() =>
        {
            var c = new HttpClient();
            c.Timeout = TimeSpan.FromSeconds(30);
            c.DefaultRequestHeaders.Clear();
            return c;
        });

        private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var s = new JsonSerializerSettings();
            s.NullValueHandling = NullValueHandling.Ignore;
            return s;
        });

        protected void AddBasicAuthenticationHeader(HttpRequestMessage m, string username, string password)
        {
            m.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
        }

        protected static string SerializeObject<T>(T value, bool allowSkipNulls)
        {
            return allowSkipNulls ? JsonConvert.SerializeObject(value, NullIgnoringJsonSerializerSettings.Value) : JsonConvert.SerializeObject(value);
        }

        protected HandleEventResult Success(string message = null, int standardThrottlingCount = 1, string outgoingRequestBody = null, string outgoingResponseBody = null)
        {
            return new HandleEventResult
            {
                Message = message ?? "The request was successful",
                Status = AffiliateReportingEventResultCode.Success,
                ThrottlingCountAndContext = standardThrottlingCount > 0 ? Tuple.Create(standardThrottlingCount, AffiliateCallbackThrottlingPolicy.StandardContextName) : null,
                OutgoingRequestBody = outgoingRequestBody,
                OutgoingResponseBody = outgoingResponseBody
            };
        }

        protected HandleEventResult Pending(string message = null, TimeSpan? waitUntilNextAttempt = null, string outgoingRequestBody = null, string outgoingResponseBody = null)
        {
            var t = waitUntilNextAttempt ?? TimeSpan.FromMinutes(30);
            return new HandleEventResult
            {
                Message = message ?? $"The request will be retried later after {t.TotalMinutes} minutes",
                Status = AffiliateReportingEventResultCode.Pending,
                WaitUntilNextAttempt = t,
                OutgoingRequestBody = outgoingRequestBody,
                OutgoingResponseBody = outgoingResponseBody
            };
        }

        protected HandleEventResult Failed(string message = null, Exception exception = null, int standardThrottlingCount = 1, string outgoingRequestBody = null, string outgoingResponseBody = null)
        {
            return new HandleEventResult
            {
                Message = message ?? $"There was an error during the request. Please contact the provider for help.",
                Status = AffiliateReportingEventResultCode.Failed,
                ThrottlingCountAndContext = standardThrottlingCount > 0 ? Tuple.Create(standardThrottlingCount, AffiliateCallbackThrottlingPolicy.StandardContextName) : null,
                OutgoingRequestBody = outgoingRequestBody,
                OutgoingResponseBody = outgoingResponseBody,
                Exception = exception
            };
        }

        protected HandleEventResult Error(string message = null, Exception exception = null, int standardThrottlingCount = 1, string outgoingRequestBody = null, string outgoingResponseBody = null)
        {
            return new HandleEventResult
            {
                Message = message ?? $"There was an error handling the request. Please contact support for help.",
                Status = AffiliateReportingEventResultCode.Error,
                ThrottlingCountAndContext = standardThrottlingCount > 0 ? Tuple.Create(standardThrottlingCount, AffiliateCallbackThrottlingPolicy.StandardContextName) : null,
                OutgoingRequestBody = outgoingRequestBody,
                OutgoingResponseBody = outgoingResponseBody,
                Exception = exception
            };
        }

        protected HandleEventResult Ignored(string message = null)
        {
            return new HandleEventResult
            {
                Message = message ?? "Ignored",
                Status = AffiliateReportingEventResultCode.Ignored
            };
        }

        protected HandleEventResult SendJsonRequest<TRequest>(
            TRequest request,
            HttpMethod httpMethod,
            string requestUrl,
            Func<HttpResponseMessage, HandleEventResult> handleResponse,
            Action<HttpRequestMessage> setupMessage = null,
            Action<string> observeJsonRequest = null)
        {
            var m = new HttpRequestMessage(httpMethod, requestUrl);
            m.Headers.Add("Accept", "application/json");
            setupMessage?.Invoke(m);
            var jsonRequest = SerializeObject(request, true);
            m.Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            observeJsonRequest?.Invoke(jsonRequest);

            HttpResponseMessage response;
            try
            {
                response = this.httpClient.Value.SendAsync(m).Result;
            }
            catch (Exception ex)
            {
                return Failed(exception: ex, outgoingRequestBody: jsonRequest);
            }

            try
            {
                return handleResponse(response);
            }
            catch (Exception ex)
            {
                return Error(exception: ex, outgoingRequestBody: jsonRequest);
            }
        }

        protected bool IsJsonResponse(HttpResponseMessage response, Action<string> observeContentType = null)
        {
            var contentType = (response.Content?.Headers?.ContentType?.MediaType ?? "").ToLowerInvariant();
            observeContentType?.Invoke(string.IsNullOrWhiteSpace(contentType) ? null : contentType);
            return contentType.Contains("application/json");
        }

        protected bool IsHtmlResponse(HttpResponseMessage response, Action<string> observeContentType = null)
        {
            var contentType = (response.Content?.Headers?.ContentType?.MediaType ?? "").ToLowerInvariant();
            observeContentType?.Invoke(string.IsNullOrWhiteSpace(contentType) ? null : contentType);
            return contentType.Contains("text/html");
        }

        protected bool HasResponseBody(HttpResponseMessage response)
        {
            return (response.Content?.Headers?.ContentLength ?? 0) > 0;
        }

        protected HandleEventResult HandleJsonResponseAsAnonymousType<T>(HttpResponseMessage response, T anonymousTypeObject, Func<T, HandleEventResult> handle, Action<string> observeJsonResponse = null, bool allowNonSuccessStatusCode = false)
        {
            if (!allowNonSuccessStatusCode)
                response.EnsureSuccessStatusCode();

            string contentType = null;
            if (!IsJsonResponse(response, observeContentType: x => contentType = x))
                return Failed(message: $"Expected application/json response but instead got: {contentType ?? "no such header"}");

            return HandleJsonResponseRaw(
                response,
                x =>
                    {
                        observeJsonResponse?.Invoke(x);
                        return handle(JsonConvert.DeserializeAnonymousType<T>(x, anonymousTypeObject));
                    },
                allowNonSuccessStatusCode: allowNonSuccessStatusCode);
        }

        protected HandleEventResult HandleJsonResponseAsType<T>(HttpResponseMessage response, Func<T, HandleEventResult> handle, Action<string> observeJsonResponse = null, bool allowNonSuccessStatusCode = false)
        {
            if (!allowNonSuccessStatusCode)
                response.EnsureSuccessStatusCode();

            string contentType = null;
            if (!IsJsonResponse(response, observeContentType: x => contentType = x))
                return Failed(message: $"Expected application/json response but instead got: {contentType ?? "no such header"}");

            return HandleJsonResponseRaw(
                response,
                x =>
                {
                    observeJsonResponse?.Invoke(x);
                    return handle(JsonConvert.DeserializeObject<T>(x));
                },
                allowNonSuccessStatusCode: allowNonSuccessStatusCode);
        }

        protected HandleEventResult HandleJsonResponseRaw(HttpResponseMessage response, Func<string, HandleEventResult> handle, bool allowNonSuccessStatusCode = false)
        {
            if (!allowNonSuccessStatusCode)
                response.EnsureSuccessStatusCode();

            string contentType = null;
            if (!IsJsonResponse(response, observeContentType: x => contentType = x))
                return Failed(message: $"Expected application/json response but instead got: {contentType ?? "no such header"}");

            return handle(response.Content.ReadAsStringAsync().Result);
        }

        protected string ReadJsonBodyIfAny(HttpResponseMessage response, Action<string> observeContentType = null)
        {
            if (!IsJsonResponse(response, observeContentType: observeContentType) || !HasResponseBody(response))
                return null;

            return response.Content.ReadAsStringAsync().Result;
        }

        protected string ReadHtmlBodyIfAny(HttpResponseMessage response, Action<string> observeContentType = null)
        {
            if (!IsHtmlResponse(response, observeContentType: observeContentType) || !HasResponseBody(response))
                return null;

            return response.Content.ReadAsStringAsync().Result;
        }

        protected T ReadJsonOrHtmlBodyIfAny<T>(HttpResponseMessage response, Func<string, T> handleJson, Func<string, T> handleHtml, Action<string> observeContentType = null)
        {
            string contentType = null;

            var json = ReadJsonBodyIfAny(response, observeContentType: x => contentType = x);
            if (json != null)
            {
                observeContentType?.Invoke(contentType);
                return handleJson(json);
            }

            var html = ReadHtmlBodyIfAny(response, observeContentType: x => contentType = x);
            if (html != null)
            {
                observeContentType?.Invoke(contentType);
                return handleHtml(html);
            }

            return default(T);
        }
    }
}