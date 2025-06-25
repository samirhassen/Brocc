
using Duende.IdentityModel.Client;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Mvc;

namespace nGccCustomerApplication.Code
{
    public class ProviderApiKeyOrBasicAuthenticationAttribute : ExternalProviderApplicationAuthenticationBase
    {
        private static Lazy<NTechSelfRefreshingBearerToken> systemUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.SystemUserCredentials));

        protected override string ErrorName => "ProviderApiKeyOrBasicAuthenticationAttribute";

        protected override AuthResult Authenticate(ActionExecutingContext filterContext, string authHeaderValue, string callerIdAddress)
        {
            if (authHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
                return AuthenticateWithApiKey(filterContext, authHeaderValue, callerIdAddress);
            else if (authHeaderValue.StartsWith("basic", StringComparison.OrdinalIgnoreCase))
                return AuthenticateWithBasicAuth(filterContext, authHeaderValue, callerIdAddress);
            else
                return null;
        }

        private bool TryParseBasicAuthHeader(string authHeaderValue, out (string UserName, string Password)? parsedAuthHeader)
        {
            try
            {
                var authTokenRaw = authHeaderValue.Substring(6); //Basic jf20rj -> jf20rj
                var authTokenPadded = authTokenRaw
                    .PadRight(4 * ((authTokenRaw.Length + 3) / 4), '='); //jf20rj -> jf20rj== ... must be multiple of 4 length
                var cred = System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(authTokenPadded)).Split(':');
                parsedAuthHeader = (UserName: cred[0], Password: cred[1]);
                return true;
            }
            catch
            {
                parsedAuthHeader = null;
                return false;
            }
        }

        private AuthResult AuthenticateWithBasicAuth(ActionExecutingContext filterContext, string authHeaderValue, string callerIdAddress)
        {
            if (!TryParseBasicAuthHeader(authHeaderValue, out var parsedAuthHeader))
                return null;

            var client = new HttpClient();
            var token = client.RequestPasswordTokenAsync(new PasswordTokenRequest()
            {
                Address = NEnv.ServiceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                ClientId = "nTechSystemUser",
                ClientSecret = "nTechSystemUser",
                UserName = parsedAuthHeader.Value.UserName,
                Password = parsedAuthHeader.Value.Password,
                Scope = "nTech1"
            });

            if (token.Result.IsError)
            {
                NLog.Error("Failed call {errorMessage}", token.Result.Error);
                return null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(parsedAuthHeader.Value.UserName))
                    return null;

                var providerName = NTechCache.WithCache($"ntech.ngcccustomerapplication.providernameByUsername.{parsedAuthHeader.Value.UserName}", TimeSpan.FromMinutes(15), () => GetProviderName(token.Result.AccessToken));

                if (string.IsNullOrWhiteSpace(providerName))
                    return null;

                return new AuthResult
                {
                    UsedApiKey = false,
                    UserName = parsedAuthHeader.Value.UserName,
                    CallerIpAddress = callerIdAddress,
                    ProviderName = providerName,
                    PreCreditBearerToken = token.Result.AccessToken
                };
            }
        }

        private class GetProviderNameResult
        {
            public bool IsProvider { get; set; }
            public bool UserExists { get; set; }
            public string ProviderName { get; set; }
        }

        private string GetProviderName(string bearerToken)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nUser"]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            AuthorizationHeaderExtensions.SetBearerToken(client, bearerToken);
            var response = client.PostAsJsonAsync("User/GetProviderNameForCurrentUser", new { }).Result;
            response.EnsureSuccessStatusCode();
            var rr = response.Content.ReadAsAsync<GetProviderNameResult>().Result;
            if (!rr.UserExists)
                throw new Exception("User does not exist");
            if (!rr.IsProvider)
                throw new Exception("User is not a provider");
            return rr.ProviderName;
        }

        private AuthResult AuthenticateWithApiKey(ActionExecutingContext filterContext, string authHeaderValue, string callerIdAddress)
        {
            var apiKey = authHeaderValue.Substring(7)?.Trim() ?? "";
            if (apiKey.Length == 0)
                return null;

            var result = NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser"), systemUser.Value.GetToken(), timeout: TimeSpan.FromMinutes(2))
                .PostJson("Api/User/ApiKeys/Authenticate", new ApiKeyAuthenticationRequest
                {
                    AuthenticationScope = "ExternalCreditApplicationApi",
                    RawApiKey = apiKey,
                    CallerIpAddress = callerIdAddress
                })
                .ParseJsonAs<ApiKeyAuthenticationResult>();

            if (result.IsAuthenticated)
            {
                return new AuthResult
                {
                    ApiKeyId = result.AuthenticatedKeyModel.Id,
                    CallerIpAddress = callerIdAddress,
                    UsedApiKey = true,
                    PreCreditBearerToken = systemUser.Value.GetToken(),
                    ProviderName = result.AuthenticatedKeyModel.ProviderName
                };
            }
            else
            {
                return null;
            }
        }

        private class ApiKeyAuthenticationRequest
        {
            public string RawApiKey { get; set; }
            public string AuthenticationScope { get; set; }
            public string CallerIpAddress { get; set; }
        }

        private class ApiKeyAuthenticationResult
        {
            public bool IsAuthenticated { get; set; }
            public string FailedAuthenticationReason { get; set; }
            public ApiKeyModel AuthenticatedKeyModel { get; set; }
        }

        private class ApiKeyModel
        {
            public int Version { get; set; }
            public string Id { get; set; }
            public string Description { get; set; }
            public string ScopeName { get; set; }
            public DateTimeOffset CreationDate { get; set; }
            public DateTimeOffset? ExpirationDate { get; set; }
            public DateTimeOffset? RevokedDate { get; set; }
            public string IpAddressFilter { get; set; }
            public string ProviderName { get; set; }
        }
    }
}