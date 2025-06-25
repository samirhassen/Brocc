using System;
using System.Collections.Generic;
using NTech.Banking.CivicRegNumbers;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.ElectronicAuthentication;

namespace nCustomer.Code.Services.EidAuthentication
{
    public class MockAuthenticationService : IEidAuthenticationService
    {
        private AuthenticationSessionService authenticationSessionService;

        public MockAuthenticationService(ICombinedClock clock)
        {
            if (NEnv.IsProduction)
                throw new Exception("Mock provider is not allowed in production");
            authenticationSessionService = new AuthenticationSessionService(clock);
        }

        public const string ProviderName = "mock";

        public CommonElectronicAuthenticationSession CreateSession(ICivicRegNumber civicRegNumber,
            ReturnUrlModel returnUrl, NtechCurrentUserMetadata currentUser, Dictionary<string, string> customData)
        {
            return authenticationSessionService.CreateSession(civicRegNumber, currentUser, customData, ProviderName,
                x =>
                {
                    var providerSessionId = Guid.NewGuid().ToString();
                    var beginLoginRedirectUrl = NEnv.ServiceRegistry.External
                        .ServiceUrl("nCustomerPages", $"mock-eid/{x.LocalSessionId}/login").ToString();
                    x.SetCustomData("standardReturnUrl",
                        returnUrl.GetReturnUrl(x).ToString()); //This is basically the provider session
                    return (ProviderSessionId: providerSessionId, BeginLoginRedirectUrl: beginLoginRedirectUrl);
                });
        }

        public (CommonElectronicAuthenticationSession Session, bool WasAuthenticated) HandleProviderLoginEvent(
            string localSessionId, NtechCurrentUserMetadata currentUser, Dictionary<string, string> providerEventData)
        {
            return authenticationSessionService.HandleProviderEvent(localSessionId, currentUser, localSession =>
            {
                var user = new CommonElectronicAuthenticationSession.AuthenticatedUserModel
                {
                    IpAddress = localSession.CustomData?.Opt("mockIpAddress"),
                    CivicRegNumber = localSession.ExpectedCivicRegNumber,
                    FirstName = localSession.CustomData?.Opt("mockFirstName"),
                    LastName = localSession.CustomData?.Opt("mockLastName"),
                    FullName = localSession.CustomData?.Opt("mockFullName")
                };
                return new AuthenticationSessionService.ProviderAuthenticationResult { AuthenticatedAsUser = user };
            });
        }
    }
}