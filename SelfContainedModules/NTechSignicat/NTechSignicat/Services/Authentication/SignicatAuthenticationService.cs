using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using NTech.Shared.Randomization;

namespace NTechSignicat.Services
{
    //https://developer.signicat.com/documentation/authentication/protocols/openid-connect/full-flow-example/
    public class SignicatAuthenticationService : SignicatAuthenticationServiceBase<SignicatAuthenticationService>
    {
        public SignicatAuthenticationService(SignicatSettings settings, ILogger<SignicatAuthenticationService> logger, IDocumentDatabaseService documentDatabaseService, IHttpClientFactory httpClientFactory, SignicatMessageEncryptionService signicatMessageEncryptionService, INEnv env) : base(settings, logger, documentDatabaseService)
        {
            this.signicatBaseUrl = settings.SignicatUrl;
            this.redirectUrl = UrlBuilder.Create(settings.SelfExternalUrl, "redirect").ToUri();
            this.clientId = settings.ClientId;
            this.clientSecret = settings.ClientSecret;
            this.httpClientFactory = httpClientFactory;
            this.signicatMessageEncryptionService = signicatMessageEncryptionService;
            this.env = env;
        }

        private readonly Uri signicatBaseUrl;
        private readonly Uri redirectUrl;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly SignicatMessageEncryptionService signicatMessageEncryptionService;
        private readonly INEnv env;

        protected override async Task<Uri> GetCustomerLoginUrl(ICivicRegNumber preFilledCivicRegNr, List<SignicatLoginMethodCode> loginMethods, string sessionId, bool requestNationalId)
        {
            if (loginMethods.Count != 1)
                throw new Exception("Currently must use exactly one login method");

            Func<UrlBuilder> createBuilder = () => UrlBuilder.Create(signicatBaseUrl, "oidc/authorize");

            var nonEncryptedUrl = createBuilder();
            Dictionary<string, object> messageEncryptionPayload = new Dictionary<string, object>();

            Action<string, string, bool, object> addParam = (name, value, encode, separateMessageEncryptionPayloadValue) =>
                {
                    messageEncryptionPayload.Add(name, separateMessageEncryptionPayloadValue ?? value);
                    nonEncryptedUrl.AddParam(name, value, encode: encode);
                };

            addParam("response_type", "code", false, null);
            var scopes = new List<string>() { "openid", "profile" };
            if (requestNationalId)
                scopes.Add("signicat.national_id");
            addParam("scope", string.Join("+", scopes), false, string.Join(" ", scopes));
            addParam("client_id", clientId, false, null);
            addParam("redirect_uri", redirectUrl.ToString(), true, null);
            addParam("state", sessionId, true, null);
            addParam("acr_values", GetLoginMethod(loginMethods.Single()), false, null);
            if (preFilledCivicRegNr != null)
            {
                var s = $"subject-{preFilledCivicRegNr.NormalizedValue}";
                addParam("login_hint", s, false, new List<string> { s });
            }

            if (settings.IsAuthenticationMessageEncryptionUsed())
            {
                var encryptedUrl = createBuilder();
                var encryptedRequest = await signicatMessageEncryptionService.EncryptOutgoingMessage(messageEncryptionPayload);
                encryptedUrl.AddParam("request", encryptedRequest, encode: true);
                return encryptedUrl.ToUri();
            }
            else
            {
                return nonEncryptedUrl.ToUri();
            }
        }

        protected override async Task<TokenSetModel> GetToken(string code, LoginSession session)
        {
            HttpClient c = httpClientFactory.CreateClient();
            c.BaseAddress = signicatBaseUrl;
            c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", clientId, clientSecret))));

            var ps = new List<KeyValuePair<string, string>>();
            Action<string, string> add = (x, y) => ps.Add(KeyValuePair.Create(x, y));

            add("client_id", clientId);
            add("redirect_uri", new Uri(session.SignicatReturnUrl).ToString());
            add("grant_type", "authorization_code");
            add("code", code);

            var result = await c.PostAsync("oidc/token", new FormUrlEncodedContent(ps));
            result.EnsureSuccessStatusCode();
            var stringResult = await result.Content.ReadAsStringAsync();
            var parsedResult = JsonConvert.DeserializeObject<TokenResult>(stringResult);
            return new TokenSetModel
            {
                AccessToken = parsedResult.access_token,
                IdToken = parsedResult.id_token,
                Scopes = new HashSet<string>(parsedResult.scope.Split(' ')),
                ExpiresDateUtc = parsedResult.expires_in.HasValue ? DateTime.UtcNow.AddSeconds(parsedResult.expires_in.Value) : new DateTime?()
            };
        }

        protected override async Task<UserInfoModel> GetUserInfo(string accessToken, LoginSession session)
        {
            HttpClient c = httpClientFactory.CreateClient();
            c.BaseAddress = signicatBaseUrl;
            c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);
            var result = await c.GetAsync("oidc/userinfo");
            result.EnsureSuccessStatusCode();
            var stringResult = await result.Content.ReadAsStringAsync();

            if (settings.AuthenticationMessageEncryptionRsaKeyFile != null)
            {
                stringResult = await signicatMessageEncryptionService.DecryptIncomingMessage(stringResult);
            }

            var parsedResult = JsonConvert.DeserializeObject<ClaimsResult>(stringResult);

            var u = new UserInfoModel
            {
                CivicRegNr = parsedResult.signicat_dot_nationalid,
                FirstName = parsedResult.given_name,
                LastName = parsedResult.family_name
            };

            if (!env.IsProduction && session.UsesTestReplacementCivicRegNr)
            {
                u.CivicRegNr = session.ExpectedCivicRegNr;
            }

            return u;
        }

        private string GetLoginMethod(SignicatLoginMethodCode signicatLoginMethod)
        {
            return settings.GetLoginMethod(signicatLoginMethod);
        }

        private class TokenResult
        {
            public string access_token { get; set; }
            public string id_token { get; set; }
            public string token_type { get; set; } //Example: Bearer
            public string scope { get; set; } //Example: openid profile
            public int? expires_in { get; set; } //Example (seconds) : 1800
        }

        private class ClaimsResult
        {
            public string sub { get; set; } //like an id
            public string name { get; set; }

            [JsonProperty(PropertyName = "signicat.national_id")]
            public string signicat_dot_nationalid { get; set; } //civic regnr

            public string given_name { get; set; } //first name
            public string locale { get; set; }
            public string family_name { get; set; } //last name
        }
    }
}