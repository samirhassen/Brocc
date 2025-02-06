using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;

namespace NTechSignicat.Services
{
    public class MockAuthenticationService : SignicatAuthenticationServiceBase<MockAuthenticationService>
    {
        public MockAuthenticationService(SignicatSettings settings, ILogger<MockAuthenticationService> logger, IDocumentDatabaseService documentDatabaseService) : base(settings, logger, documentDatabaseService)
        {
        }

        protected override Task<Uri> GetCustomerLoginUrl(ICivicRegNumber preFilledCivicRegNr, List<SignicatLoginMethodCode> loginMethods, string sessionId, bool requestNationalId)
        {
            return Task.FromResult(NTechServiceRegistry.CreateUrl(
                settings.SelfExternalUrl,
                "mock-authenticate",
                Tuple.Create("sessionId", sessionId)));
        }

        protected override Task<TokenSetModel> GetToken(string code, LoginSession session)
        {
            return Task.FromResult(new TokenSetModel
            {
                AccessToken = "mockAccessToken" + session.Id,
                ExpiresDateUtc = DateTime.UtcNow.AddHours(1),
                IdToken = "mockIdToken" + session.Id,
                Scopes = new HashSet<string> { "openid", "profile" }
            });
        }

        protected override Task<UserInfoModel> GetUserInfo(string accessToken, LoginSession session)
        {
            return Task.FromResult(new UserInfoModel
            {
                CivicRegNr = session.ExpectedCivicRegNr,
                FirstName = session.CustomData?.GetValueOrDefault("MockFirstName") ?? "Test",
                LastName = session.CustomData?.GetValueOrDefault("MockLastName") ?? "Person"
            });
        }
    }
}
