using nCustomer.Code.Services.EidAuthentication.EncryptionHelpers;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace nCustomer.Code.Services.EidAuthentication
{
    public class Signicat2AuthenticationService : IEidAuthenticationService
    {
        private readonly NTechSimpleSettings settings;
        private readonly AuthenticationSessionService authenticationSessionService;
        private readonly JweEncryptionService encryptionService;

        public Signicat2AuthenticationService(NTechSimpleSettings settings, ICombinedClock clock)
        {
            authenticationSessionService = new AuthenticationSessionService(clock);
            encryptionService = new JweEncryptionService(settings);
            this.settings = settings;
        }

        public static string ProviderName = "signicat2";
        private static string CountryIsoCode => NEnv.ClientCfg.Country.BaseCountry;
        private static readonly IServiceClientSyncConverter ServiceClientSyncConverter = new ServiceClientSyncConverterLegacy();
        public TResult ToSync<TResult>(Func<Task<TResult>> action) => ServiceClientSyncConverter.ToSync(action);
        private bool IsEncryptionEnabled => GetIsEncryptionEnabled();

        public CommonElectronicAuthenticationSession CreateSession(ICivicRegNumber civicRegNumber, ReturnUrlModel returnUrl, NtechCurrentUserMetadata currentUser, Dictionary<string, string> customData)
        {
            return authenticationSessionService.CreateSession(civicRegNumber, currentUser, customData, ProviderName, (Func<CommonElectronicAuthenticationSession, (string ProviderSessionId, string BeginLoginRedirectUrl)>)(x =>
            {
                var bearerToken = ToSync(() => GetToken());
                var redirectUrl = returnUrl.GetReturnUrl(x).ToString();

                var baseUrl = settings.Req("baseUrl");
                var accountId = settings.Req("accountId");
                var encryptionPublicKey = IsEncryptionEnabled ? encryptionService.GetEncryptionPublicKey() : null;

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                var requestData = new
                {
                    prefilledInput = new
                    {
                        nin = civicRegNumber.NormalizedValue,
                    },
                    callbackUrls = new
                    {
                        success = redirectUrl,
                        abort = redirectUrl,
                        error = redirectUrl
                    },
                    allowedProviders = GetElectronicIdLoginMethod(),
                    language = GetLangFromCountry(),
                    flow = "redirect",
                    requestedAttributes = new string[] { "firstName", "lastName" },
                    encryptionPublicKey
                };

                var jsonPayload = JsonConvert.SerializeObject(requestData);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = ToSync(() => httpClient.PostAsync($"{baseUrl}/auth/rest/sessions?signicat-accountId={accountId}", httpContent));
                response.EnsureSuccessStatusCode();

                var responseBody = ToSync(() => response.Content.ReadAsStringAsync());
                var payload = IsEncryptionEnabled ? encryptionService.GetDecryptedPayload(responseBody) : responseBody;
                var sessionObject = JsonConvert.DeserializeObject<Signicat2Session>(payload);
                return (ProviderSessionId: sessionObject.id, BeginLoginRedirectUrl: sessionObject.authenticationUrl);
            }));
        }

        private async Task<string> GetToken()
        {
            var baseUrl = settings.Req("baseUrl");
            var clientId = settings.Req("clientId");
            var clientSecret = settings.Req("clientSecret");

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/auth/open/connect/token");
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

            request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "signicat-api"),
                new KeyValuePair<string, string>("client_id", clientId)
            });
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var stringResult = await response.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<TokenResult>(stringResult);

            return token.access_token;
        }

        public (CommonElectronicAuthenticationSession Session, bool WasAuthenticated) HandleProviderLoginEvent(string localSessionId, NtechCurrentUserMetadata currentUser, Dictionary<string, string> providerEventData)
        {
            return authenticationSessionService.HandleProviderEvent(localSessionId, currentUser, localSession =>
            {
                var session = ToSync(() => GetSession(localSession.ProviderSessionId));
                if (session == null)
                    return new AuthenticationSessionService.ProviderAuthenticationResult { FailedMessage = "Session missing from provider" };

                if (session.status == HandleProviderLoginEventStatus.SUCCESS.ToString())
                {
                    var user = new CommonElectronicAuthenticationSession.AuthenticatedUserModel
                    {
                        IpAddress = null,
                        CivicRegNumber = NEnv.BaseCivicRegNumberParser.Parse(session.subject?.idpId).NormalizedValue,
                        FirstName = session.subject?.firstName,
                        LastName = session.subject?.lastName
                    };

                    return new AuthenticationSessionService.ProviderAuthenticationResult { AuthenticatedAsUser = user };
                }
                else
                {
                    return new AuthenticationSessionService.ProviderAuthenticationResult { FailedMessage = session.status };
                }
            });
        }

        private async Task<GetSessionResult> GetSession(string id)
        {
            var session = await PostOrGet<object>($"/auth/rest/sessions/{id}", null, HttpMethod.Get);
            return JsonConvert.DeserializeObject<GetSessionResult>(session);
        }

        private async Task<string> PostOrGet<TResult>(string relativeUrl, object requestData, HttpMethod method)
        {
            var token = await GetToken();

            var client = new HttpClient();
            var baseUrl = settings.Req("baseUrl");
            var request = new HttpRequestMessage(method, $"{baseUrl}{relativeUrl}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            return IsEncryptionEnabled ? encryptionService.GetDecryptedPayload(responseStr) : responseStr; 
        }

        private bool GetIsEncryptionEnabled()
        {
            var isEnabled = settings.OptBool("isEncryptionEnabled");
            if (!isEnabled && CountryIsoCode == "FI")
            {
                NLog.Error("Error when fetching encryption settings. Encryption is not enabled but is required for FTN.");
                throw new Exception("FTN requires encryption settings.");
            }

            if (isEnabled && string.IsNullOrEmpty(settings.Opt("authenticationMessageEncryptionRsaKeyFile")))
            {
                NLog.Error("Error when fetching encryption settings. Setting 'authenticationMessageEncryptionRsaKeyFile' is required for enabled encryption.");
                throw new Exception("Missing encryption keys.");
            }

            return isEnabled;
        }

        private string[] GetElectronicIdLoginMethod()
        {
            if (CountryIsoCode == "FI")
                return new string[] { "ftn" };
            else if (CountryIsoCode == "SE")
                return new string[] { "sbid" };
            else
                throw new NotImplementedException();
        }

        private string GetLangFromCountry()
        {
            if (CountryIsoCode == "FI")
                return "fi";
            else if (CountryIsoCode == "SE")
                return "sv";
            else
                return "en";
        }

        private class TokenResult
        {
            public string access_token { get; set; }
        }

        private class Signicat2Session
        {
            public string id { get; set; }
            public string authenticationUrl { get; set; }
        }

        private enum HandleProviderLoginEventStatus
        {
            SUCCESS, ABORT, ERROR
        }

        private class GetSessionResult
        {
            public string status { get; set; }

            public SubjectModel subject { get; set; }

            public EnvironmentModel environment { get; set; }

            public class SubjectModel
            {
                public string idpId { get; set; } //national id
                public string firstName { get; set; }
                public string lastName { get; set; }
            }

            public class EnvironmentModel
            {
                public string ipAddress { get; set; }
            }
        }
    }
}