using Duende.IdentityModel.Client;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Net.Http;

namespace nCustomerPages.Code
{
    public class ApiKeyOrBearerTokenAuthHelper
    {
        private static Lazy<NTechSelfRefreshingBearerToken> systemUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.SystemUserUserNameAndPassword));

        public AuthResult AuthenticateWithBasicAuth(string username, string password, string callerIpAddress, bool requireProvider)
        {
          
            var client = new HttpClient();
            var token = client.RequestPasswordTokenAsync(new PasswordTokenRequest()
            {
                Address = NEnv.ServiceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                ClientId = "nTechSystemUser",
                ClientSecret = "nTechSystemUser",
                UserName = username,
                Password = password,
                Scope = "nTech1"
            });
            if (token.Result.IsError)
            {
                NLog.Error("Failed call {errorMessage}", token.Result.Error);
                return null;
            }

            string providerName = null;
            if (requireProvider)
            {
                var providerNameResult = NTechCache.WithCache($"ntech.customerpages.providernameByUsername2.{username}", TimeSpan.FromMinutes(15), () => GetProviderName(token.Result.AccessToken));

                if (providerNameResult?.IsProvider != true || string.IsNullOrWhiteSpace(providerNameResult?.ProviderName))
                    return null;

                providerName = providerNameResult.ProviderName;
            }

            return new AuthResult
            {
                CallerIpAddress = callerIpAddress,
                UsedApiKey = false,
                PreCreditBearerToken = token.Result.AccessToken,
                ProviderName = providerName,
                UserName = username
            };
        }


        public AuthResult AuthenticateWithApiKey(string apiKey, string callerIdAddress, string scopeName)
        {
            var result = NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser"), systemUser.Value.GetToken(), timeout: TimeSpan.FromMinutes(2))
                .PostJson("Api/User/ApiKeys/Authenticate", new ApiKeyAuthenticationRequest
                {
                    AuthenticationScope = scopeName,
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

        public class GetProviderNameResult
        {
            public bool IsProvider { get; set; }
            public bool IsSystemUser { get; set; }
            public bool UserExists { get; set; }
            public string ProviderName { get; set; }
        }

        public GetProviderNameResult GetProviderName(string bearerToken)
        {
            return NHttp
                 .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser"), bearerToken)
                 .PostJson("User/GetProviderNameForCurrentUser", new { })
                 .ParseJsonAs<GetProviderNameResult>();
        }

        public class AuthResult
        {
            public string ProviderName { get; set; }
            public bool UsedApiKey { get; set; }
            public string UserName { get; set; }
            public string ApiKeyId { get; set; }
            public string CallerIpAddress { get; set; }
            public string PreCreditBearerToken { get; set; }
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