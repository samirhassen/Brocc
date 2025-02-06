using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.User.Database;
using NTech.Core.User.Shared;
using NTech.Core.User.Shared.Services;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void AddAndAuthenticateWithApiKey(UlLegacyTestRunner.TestSupport support)
        {
            var service = new ApiKeyService(
                new UserContextFactory(() =>
                    new UserContextExtended(support.CurrentUser, support.Clock)));

            var key = service.CreateApiKey(new CreateApiKeyRequest
            {
                Description = "test",
                ScopeName = "a"
            });

            var result = service.Authenticate(new User.Shared.Services.ApiKeyAuthenticationRequest
            {
                AuthenticationScope = "a",
                RawApiKey = key.RawApiKey
            });

            Assert.That(result.IsAuthenticated, Is.EqualTo(true));
        }

    }
}