using Microsoft.AspNetCore.Http;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Module.Infrastrucutre
{
    public class NHttpServiceHttpContextUser : INHttpServiceUser
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public NHttpServiceHttpContextUser(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetBearerToken()
        {
            var httpContext = httpContextAccessor.HttpContext;
            return NHttpServiceDirectUser.GetUserBearerToken(httpContext?.User?.Identity, true);
        }

        public Task<string> GetBearerTokenAsync() => Task.FromResult(GetBearerToken());
    }
}