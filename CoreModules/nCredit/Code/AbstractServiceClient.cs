using NTech.Services.Infrastructure;
using System;

namespace nCredit.Code
{
    public abstract class AbstractServiceClient
    {
        protected abstract string ServiceName { get; }

        protected virtual string GetCurrentAccessToken() => NHttp.GetCurrentAccessToken();

        protected NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal[ServiceName]), bearerToken ?? GetCurrentAccessToken(), timeout: timeout);
        }
    }

    public abstract class AbstractSystemUserServiceClient : AbstractServiceClient
    {
        private Lazy<NTechSelfRefreshingBearerToken> systemUserToken = new Lazy<NTechSelfRefreshingBearerToken>(() =>
        {
            var a = NEnv.ApplicationAutomationUsernameAndPassword;
            return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, a.Item1, a.Item2);
        });

        protected override string GetCurrentAccessToken()
        {
            return systemUserToken.Value.GetToken();
        }
    }
}