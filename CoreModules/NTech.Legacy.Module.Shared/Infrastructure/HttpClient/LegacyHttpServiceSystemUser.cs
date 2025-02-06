using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Threading.Tasks;

namespace NTech.Legacy.Module.Shared.Infrastructure.HttpClient
{
    public class LegacyHttpServiceSystemUser : INHttpServiceUser
    {
        private static Lazy<NTechSelfRefreshingBearerToken> selfRefreshingBearerToken => new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(
                NTechEnvironment.Instance.ServiceRegistry,
                NTechEnvironment.Instance.Setting("ntech.automationuser.username", true),
                NTechEnvironment.Instance.Setting("ntech.automationuser.password", true)));

        private static Lazy<LegacyHttpServiceSystemUser> sharedInstance => new Lazy<LegacyHttpServiceSystemUser>(() => new LegacyHttpServiceSystemUser());
        public static INHttpServiceUser SharedInstance => sharedInstance.Value;

        public LegacyHttpServiceSystemUser()
        {
        }

        public string GetBearerToken() => selfRefreshingBearerToken.Value.GetToken();
        public Task<string> GetBearerTokenAsync() => Task.FromResult(selfRefreshingBearerToken.Value.GetToken());
    }
}