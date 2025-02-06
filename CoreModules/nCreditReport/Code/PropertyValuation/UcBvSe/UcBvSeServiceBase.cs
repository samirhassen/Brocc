using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace nCreditReport.Code.PropertyValuation.UcBvSe
{
    public abstract class UcBvSeServiceBase
    {
        private readonly string username;
        private readonly string password;
        private readonly Uri serviceEndpoint;
        private readonly string clientIp;

        private string accessKey;
        private DateTime? accessKeyExpirationDate;

        public UcBvSeServiceBase(NTechSimpleSettings settings) : this(
            new Uri(settings.Req("endpoint")),
            settings.Req("username"),
            settings.Req("password"),
            settings.Opt("clientIp"))
        {

        }

        public UcBvSeServiceBase(Uri serviceEndpoint, string username, string password, string clientIp)
        {
            this.username = username;
            this.password = password;
            this.serviceEndpoint = serviceEndpoint;
            this.accessKey = null;
            this.accessKeyExpirationDate = null;
            this.clientIp = clientIp ?? "::1";
        }

        protected async Task<ServiceResponse<TResponse>> PostJsonRequestAndResponse<TResponse>(object request, string relativeUrl)
        {
            using (var client = new HttpClient())
            {
                var result = await PostWithJsonResponse<TResponse>(request, client, relativeUrl, false);

                if (result.Felkod == 2 && result.Felmeddelande == "NoAccess")
                {
                    result = await PostWithJsonResponse<TResponse>(request, client, relativeUrl, true);
                }

                return result;
            }
        }

        protected async Task<(bool IsOk, byte[] Result, int? HttpErrorCode, string HttpErrorStatusText)> PostJsonRequestAndOctetStreamResponse(object request, string relativeUrl)
        {
            using (var client = new HttpClient())
            {
                var result = await PostJsonRequest(request, client, relativeUrl, false);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    result = await PostJsonRequest(request, client, relativeUrl, true);
                }
                if (!result.IsSuccessStatusCode)
                {
                    var foo = await result.Content.ReadAsStringAsync();
                    return (IsOk: false, Result: null, HttpErrorCode: (int)result.StatusCode, HttpErrorStatusText: result.ReasonPhrase);
                }


                result.EnsureSuccessStatusCode();

                var data = await result.Content.ReadAsByteArrayAsync();
                return (IsOk: true, Result: data, HttpErrorCode: null, HttpErrorStatusText: null);
            }
        }

        private async Task<ServiceResponse<TResponse>> PostWithJsonResponse<TResponse>(object request, HttpClient client, string relativeUrl, bool forceNewAccessKey, Action<string> injectRawJson = null)
        {
            var response = await PostJsonRequest(request, client, relativeUrl, forceNewAccessKey);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<ServiceResponse<TResponse>>(jsonResponse);

            parsedResponse.RawFullJson = jsonResponse;

            return parsedResponse;
        }

        private async Task<HttpResponseMessage> PostJsonRequest(
            object request, HttpClient client, string relativeUrl, bool forceNewAccessKey)
        {
            var urlExact = new Uri(serviceEndpoint, relativeUrl).ToString();
            var message = new HttpRequestMessage(HttpMethod.Post, urlExact);
            var rawRequest = JsonConvert.SerializeObject(request, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            message.Content = new StringContent(rawRequest, System.Text.Encoding.UTF8, "application/json");
            var accessKey = await GetAccessKey(client, forceNewAccessKey);
            message.Headers.Add("AccessKey", accessKey);
            message.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(message);

            return response;
        }

        private async Task<string> GetAccessKey(HttpClient client, bool force)
        {
            if (force || accessKey == null || (accessKeyExpirationDate.HasValue && accessKeyExpirationDate.Value < DateTime.UtcNow))
            {
                var message = new HttpRequestMessage(HttpMethod.Post, new Uri(serviceEndpoint, "ValidateUser/Login").ToString());
                message.Headers.Add("Accept", "application/json");
                message.Content = new StringContent(SerializeObject(new ValidateUserRequest
                {
                    Username = username,
                    Password = password,
                    ClientIP = clientIp
                }), System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(message);

                response.EnsureSuccessStatusCode();
                var stringResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ValidateUserResponse>(stringResponse);

                if (!string.IsNullOrWhiteSpace(result?.Message))
                    throw new Exception($"Error: {result.Message}");
                if (string.IsNullOrWhiteSpace(result.AccessKey))
                    throw new Exception($"Error: No access key returned");

                this.accessKey = result.AccessKey;
                this.accessKeyExpirationDate = DateTime.UtcNow.AddMinutes(20); //Docs say 30 minutes minimum
            }

            return this.accessKey;
        }

        private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var s = new JsonSerializerSettings();
            s.NullValueHandling = NullValueHandling.Ignore;
            return s;
        });

        private static string SerializeObject<T>(T value, bool skipNulls = true)
        {
            //UCs api seems a bit iffy in handling null properties so better to just leave them out.
            return skipNulls ? JsonConvert.SerializeObject(value, NullIgnoringJsonSerializerSettings.Value) : JsonConvert.SerializeObject(value);
        }

        private class ValidateUserRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string ClientIP { get; set; }
        }

        private class ValidateUserResponse
        {
            public string Message { get; set; }
            public string Givenname { get; set; }
            public string Familyname { get; set; }
            public string AccessKey { get; set; }
        }
    }
}