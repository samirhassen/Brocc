using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace nCustomerPages.Code
{
    public class ProviderApiBaseHelper
    {
        private RawJsonActionResult WithRequestLogging(HttpContextBase httpContext, Lazy<RotatingLogFile> requestLog, string currentProviderName, string methodPath, string rawRequest, Func<RawJsonActionResult> handleRequest, string httpMethodName)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - {httpContext?.GetOwinContext()?.Request?.RemoteIpAddress} - {currentProviderName}: {httpMethodName} {methodPath}");
            logEntry.AppendLine("Request:");
            logEntry.AppendLine(rawRequest);

            try
            {
                var result = handleRequest();
                logEntry.AppendLine("Response: ");
                logEntry.AppendLine(result.JsonData);
                requestLog.Value.Log(logEntry.ToString());

                return result;
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                logEntry.AppendLine("Error: ");
                logEntry.AppendLine($"Log correlation id: {correlationId}");
                requestLog.Value.Log(logEntry.ToString());
                throw new Exception($"Provider api error {correlationId}", ex);
            }
        }

        public RawJsonActionResult ForwardApiRequest(string targetModule, string relativePath, JObject requestObject)
        {
            var s = NEnv.ServiceRegistry;

            return NHttp
                .Begin(s.Internal.ServiceRootUri(targetModule), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                .PostJsonRaw(relativePath, requestObject.ToString())
                .HandlingApiErrorWithHttpCode(x => new RawJsonActionResult
                {
                    JsonData = x.ParseAsRawJson()
                }, (err, statusCode) => CreateError(statusCode, err?.ErrorCode, err?.ErrorMessage));
        }

        public RawJsonActionResult CreateError(System.Net.HttpStatusCode httpStatusCode, string errorCode, string errorMessage)
        {
            return new RawJsonActionResult
            {
                CustomHttpStatusCode = (int)httpStatusCode,
                CustomStatusDescription = errorCode,
                JsonData = JsonConvert.SerializeObject(new { errorCode = errorCode, errorMessage = errorMessage }),
                IsNTechApiError = true
            };
        }

        public RawJsonActionResult WithRequestAsJObject(HttpRequestBase request, HttpContextBase httpContext, Lazy<RotatingLogFile> requestLog, string currentProviderName, bool isApiLoggingEnabled, string methodPath, Func<JObject, RawJsonActionResult> f, string httpMethodName = "POST")
        {
            if (!request.ContentType.Contains("application/json"))
            {
                return CreateError(HttpStatusCode.BadRequest, "invalidContentType", "Invalid content type. Must be application/json");
            }

            request.InputStream.Position = 0;
            if (request.InputStream.Length == 0)
            {
                return CreateError(HttpStatusCode.BadRequest, "missingRequestBody", "Missing request body");
            }

            using (var r = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                var requestString = r.ReadToEnd();
                Func<RawJsonActionResult> handle = () => f(JObject.Parse(requestString));

                if (NEnv.IsVerboseLoggingEnabled || isApiLoggingEnabled)
                {
                    return WithRequestLogging(httpContext, requestLog, currentProviderName, methodPath, requestString, handle, httpMethodName);
                }
                else
                    return handle();
            }
        }
    }
}