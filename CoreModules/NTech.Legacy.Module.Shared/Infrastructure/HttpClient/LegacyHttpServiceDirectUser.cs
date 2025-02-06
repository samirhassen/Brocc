using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Security.Principal;
using System.Threading.Tasks;

namespace NTech.Legacy.Module.Shared.Infrastructure.HttpClient
{
    public class LegacyHttpServiceDirectUser : INHttpServiceUser
    {
        private readonly IIdentity user;

        public LegacyHttpServiceDirectUser(IIdentity user)
        {
            this.user = user;
        }

        public static string GetUserBearerToken(IIdentity identity, bool isRequired)
        {
            var claimsIdentity = identity as System.Security.Claims.ClaimsIdentity;
            var accessToken = claimsIdentity?.FindFirst("access_token")?.Value;

            if (accessToken == null && isRequired)
                throw new Exception("Missing access token");

            return accessToken;
        }

        public string GetBearerToken() => GetUserBearerToken(user, true);

        public Task<string> GetBearerTokenAsync() => Task.FromResult(GetBearerToken());
    }
}