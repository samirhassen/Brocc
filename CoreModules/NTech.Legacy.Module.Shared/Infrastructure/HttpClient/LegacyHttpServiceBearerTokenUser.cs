using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Threading.Tasks;

namespace NTech.Legacy.Module.Shared.Infrastructure.HttpClient
{
    public class LegacyHttpServiceBearerTokenUser : INHttpServiceUser
    {
        private readonly Lazy<NTechSelfRefreshingBearerToken> selfRefreshingBearerToken;
        public LegacyHttpServiceBearerTokenUser(Lazy<NTechSelfRefreshingBearerToken> selfRefreshingBearerToken)
        {
            this.selfRefreshingBearerToken = selfRefreshingBearerToken;
        }

        public string GetBearerToken() => selfRefreshingBearerToken.Value.GetToken();
        public Task<string> GetBearerTokenAsync() => Task.FromResult(selfRefreshingBearerToken.Value.GetToken());
    }
}