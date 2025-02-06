using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public interface IUserClient
    {
        Task<ApiKeyAuthenticationResult> AuthenticateWithApiKeyAsync(ApiKeyAuthenticationRequest request);
        ApiKeyAuthenticationResult AuthenticateWithApiKey(ApiKeyAuthenticationRequest request);
        Task<Dictionary<string, string>> GetUserDisplayNamesByUserIdAsync();
        Dictionary<string, string> GetUserDisplayNamesByUserId();
    }

    public class ApiKeyAuthenticationRequest
    {
        public string RawApiKey { get; set; }
        public string AuthenticationScope { get; set; }
        public string CallerIpAddress { get; set; }
    }

    public class ApiKeyAuthenticationResult
    {
        public bool IsAuthenticated { get; set; }
        public string FailedAuthenticationReason { get; set; }
        public ApiKeyModel AuthenticatedKeyModel { get; set; }
    }

    public class ApiKeyModel
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
