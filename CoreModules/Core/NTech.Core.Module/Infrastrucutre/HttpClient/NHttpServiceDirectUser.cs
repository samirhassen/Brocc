using NTech.Core.Module.Shared.Infrastructure;
using System.Security.Principal;

namespace NTech.Core.Module.Infrastrucutre
{
    public class NHttpServiceDirectUser : INHttpServiceUser
    {
        private readonly IIdentity user;

        public NHttpServiceDirectUser(IIdentity user)
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