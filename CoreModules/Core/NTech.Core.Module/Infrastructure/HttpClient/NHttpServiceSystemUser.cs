using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Module.Infrastrucutre
{
    public class NHttpServiceSystemUser : INHttpServiceUser
    {
        public NHttpServiceSystemUser(NTechSelfRefreshingBearerToken systemUserToken)
        {
            selfRefreshingBearerToken = new Lazy<NTechSelfRefreshingBearerToken>(() => systemUserToken);
        }
        public NHttpServiceSystemUser(IHttpClientFactory httpClientFactory, NEnv env)
        {
            selfRefreshingBearerToken = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(httpClientFactory, env.ServiceRegistry,
                env.RequiredSetting("ntech.automationuser.username"),
                env.RequiredSetting("ntech.automationuser.password"))
            );
        }
        private readonly Lazy<NTechSelfRefreshingBearerToken> selfRefreshingBearerToken;

        public string GetBearerToken() => selfRefreshingBearerToken.Value.GetToken();
        public Task<string> GetBearerTokenAsync() => selfRefreshingBearerToken.Value.GetTokenAsync();
    }
}