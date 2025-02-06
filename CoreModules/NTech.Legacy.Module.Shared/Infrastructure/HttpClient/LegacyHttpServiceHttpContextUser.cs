using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Threading.Tasks;
using System.Web;

namespace NTech.Legacy.Module.Shared.Infrastructure.HttpClient
{
    public class LegacyHttpServiceHttpContextUser : INHttpServiceUser
    {
        public LegacyHttpServiceHttpContextUser()
        {

        }

        private static Lazy<LegacyHttpServiceHttpContextUser> sharedInstance => new Lazy<LegacyHttpServiceHttpContextUser>(() => new LegacyHttpServiceHttpContextUser());
        public static INHttpServiceUser SharedInstance => sharedInstance.Value;

        public string GetBearerToken()
        {
            string accessToken = null;

            var user = (HttpContext.Current?.User?.Identity) as System.Security.Claims.ClaimsIdentity;

            if (user != null)
            {
                accessToken = user?.FindFirst("access_token")?.Value;
            }

            if (accessToken == null)
            {
                var h = HttpContext.Current?.Request?.Headers["Authorization"];
                if (h != null && h.StartsWith("Bearer"))
                {
                    accessToken = h.Substring("Bearer".Length).Trim();
                }
            }

            if (accessToken == null)
                throw new Exception("Missing access token");

            return accessToken;
        }

        public Task<string> GetBearerTokenAsync() => Task.FromResult(GetBearerToken());
    }
}