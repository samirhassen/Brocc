using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;

namespace NTech.Services.Infrastructure
{
    public class RestClient
    {
        private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var s = new JsonSerializerSettings();
            s.NullValueHandling = NullValueHandling.Ignore;
            return s;
        });

        public System.Security.Cryptography.X509Certificates.X509Certificate2 LoadClientCertificateUsingThumbPrint(string certificateThumbPrint)
        {
            using (var keyStore = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine))
            {
                keyStore.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
                return keyStore
                    .Certificates
                    .OfType<System.Security.Cryptography.X509Certificates.X509Certificate2>()
                    .First(x => x.Thumbprint.Equals(certificateThumbPrint, StringComparison.OrdinalIgnoreCase));
            }
        }

        public System.Security.Cryptography.X509Certificates.X509Certificate2 LoadClientCertificateFromFile(string certificateFilename, string certificatePassword = null)
        {
            if (certificatePassword != null)
                return new System.Security.Cryptography.X509Certificates.X509Certificate2(System.IO.File.ReadAllBytes(certificateFilename), certificatePassword);
            else
                return new System.Security.Cryptography.X509Certificates.X509Certificate2(System.IO.File.ReadAllBytes(certificateFilename));
        }

        public Result SendRequest<TRequest>(
                            TRequest request,
                            HttpMethod httpMethod,
                            string requestUrl,
                            Action<HttpRequestMessage> setupMessage = null,
                            Action<string> observeJsonRequest = null,
                            System.Security.Cryptography.X509Certificates.X509Certificate2 clientCertificate = null,
                            string bearerToken = null,
                            TimeSpan? timeout = null)
        {
            var m = new HttpRequestMessage(httpMethod, requestUrl);
            m.Headers.Add("Accept", "application/json");
            setupMessage?.Invoke(m);
            var jsonRequest = JsonConvert.SerializeObject(request, NullIgnoringJsonSerializerSettings.Value);
            observeJsonRequest?.Invoke(jsonRequest);
            m.Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            HttpClient c;
            if (clientCertificate != null)
            {
                var h = new HttpClientHandler();
                h.ClientCertificates.Add(clientCertificate);
                h.ClientCertificateOptions = ClientCertificateOption.Manual;
                c = new HttpClient(h);
            }
            else
            {
                c = new HttpClient();
            }

            c.Timeout = timeout ?? TimeSpan.FromSeconds(30);
            c.DefaultRequestHeaders.Clear();

            if (bearerToken != null)
                c.SetBearerToken(bearerToken);

            var response = c.SendAsync(m).Result;

            return new Result { Response = response };
        }

        public class Result
        {
            public HttpResponseMessage Response { get; set; }

            public string ReadJsonBodyIfAny(Action<string> observeContentType = null)
            {
                if (!IsJsonResponse(observeContentType: observeContentType) || !HasResponseBody())
                    return null;

                return Response.Content.ReadAsStringAsync().Result;
            }

            protected bool IsJsonResponse(Action<string> observeContentType = null)
            {
                var contentType = (Response.Content?.Headers?.ContentType?.MediaType ?? "").ToLowerInvariant();
                observeContentType?.Invoke(string.IsNullOrWhiteSpace(contentType) ? null : contentType);
                return contentType.Contains("application/json");
            }

            protected bool IsHtmlResponse(Action<string> observeContentType = null)
            {
                var contentType = (Response.Content?.Headers?.ContentType?.MediaType ?? "").ToLowerInvariant();
                observeContentType?.Invoke(string.IsNullOrWhiteSpace(contentType) ? null : contentType);
                return contentType.Contains("text/html");
            }

            protected bool HasResponseBody()
            {
                return (Response.Content?.Headers?.ContentLength ?? 0) > 0;
            }

            public string ReadHtmlBodyIfAny(Action<string> observeContentType = null)
            {
                if (!IsHtmlResponse(observeContentType: observeContentType) || !HasResponseBody())
                    return null;

                return Response.Content.ReadAsStringAsync().Result;
            }

            public T ReadJsonOrHtmlBodyIfAny<T>(Func<string, T> handleJson, Func<string, T> handleHtml, Action<string> observeContentType = null)
            {
                string contentType = null;

                var json = ReadJsonBodyIfAny(observeContentType: x => contentType = x);
                if (json != null)
                {
                    observeContentType?.Invoke(contentType);
                    return handleJson(json);
                }

                var html = ReadHtmlBodyIfAny(observeContentType: x => contentType = x);
                if (html != null)
                {
                    observeContentType?.Invoke(contentType);
                    return handleHtml(html);
                }

                return default(T);
            }
        }
    }
}