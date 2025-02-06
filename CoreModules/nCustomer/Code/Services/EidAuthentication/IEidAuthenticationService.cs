using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using System.Collections.Generic;

namespace nCustomer.Code.Services.EidAuthentication
{
    public interface IEidAuthenticationService
    {
        CommonElectronicAuthenticationSession CreateSession(ICivicRegNumber civicRegNumber, ReturnUrlModel returnUrl, NtechCurrentUserMetadata currentUser, Dictionary<string, string> customData);
        (CommonElectronicAuthenticationSession Session, bool WasAuthenticated) HandleProviderLoginEvent(string localSessionId, NtechCurrentUserMetadata currentUser, Dictionary<string, string> providerEventData);
    }
}
