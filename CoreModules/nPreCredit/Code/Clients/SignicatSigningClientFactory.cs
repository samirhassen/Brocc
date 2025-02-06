using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;

namespace nPreCredit.Code.Clients
{
    public static class SignicatSigningClientFactory
    {
        private static Lazy<NTechSelfRefreshingBearerToken> signatureUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(
                NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword));

        public static SignicatSigningClient CreateClient()
        {
            var clientFactory = LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry);
            return new SignicatSigningClient(clientFactory, new LegacyHttpServiceBearerTokenUser(signatureUser), NEnv.ClientCfgCore);
        }
    }
}