using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class UserClient : IUserClient
    {
        private ServiceClient client;
        public UserClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nUser");
        }

        public Task<ApiKeyAuthenticationResult> AuthenticateWithApiKeyAsync(ApiKeyAuthenticationRequest request) => client.Call(
            x => x.PostJson("Api/User/ApiKeys/Authenticate", request),
            x => x.ParseJsonAs<ApiKeyAuthenticationResult>());

        public ApiKeyAuthenticationResult AuthenticateWithApiKey(ApiKeyAuthenticationRequest request) => client.ToSync(() => AuthenticateWithApiKeyAsync(request));

        public async Task<Dictionary<string, string>> GetUserDisplayNamesByUserIdAsync()
        {
            var result = await client.Call(
                x => x.PostJson("User/GetAllDisplayNamesAndUserIds", new { }),
                x => x.ParseJsonAs<GetUserDisplayNamesByUserIdResult[]>()
                );
            return result.ToDictionary(x => x.UserId, x => x.DisplayName);
        }

        public Dictionary<string, string> GetUserDisplayNamesByUserId() =>
            client.ToSync(() => GetUserDisplayNamesByUserIdAsync());

        private class GetUserDisplayNamesByUserIdResult
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
