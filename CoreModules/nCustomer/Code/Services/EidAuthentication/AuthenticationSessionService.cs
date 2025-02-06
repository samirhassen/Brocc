using nCustomer.DbModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Customer.Shared.Services.Utilities;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services.EidAuthentication
{
    public class AuthenticationSessionService
    {
        public AuthenticationSessionService(ICoreClock clock)
        {
            sessionStore = new SessionStore<CommonElectronicAuthenticationSession>("AuthenticationServiceV1", "AuthenticationServiceArchiveDateV1", "AuthenticationServiceAlternateKeyV1", clock,
                x => x.LocalSessionId,
                () => new CustomersContext());
        }

        private readonly SessionStore<CommonElectronicAuthenticationSession> sessionStore;

        public class ProviderAuthenticationResult
        {
            public CommonElectronicAuthenticationSession.AuthenticatedUserModel AuthenticatedAsUser { get; set; }
            public string FailedMessage { get; set; }
        }

        public (CommonElectronicAuthenticationSession Session, bool WasAuthenticated) HandleProviderEvent(
            string localSessionId,
            NtechCurrentUserMetadata currentUser,
            Func<CommonElectronicAuthenticationSession, ProviderAuthenticationResult> getProviderResult)
        {
            var localSession = GetSession(localSessionId);
            if (localSession == null || localSession.IsClosed)
                return (Session: localSession, WasAuthenticated: false);

            var providerResult = getProviderResult(localSession);

            localSession.IsClosed = true;
            if (providerResult.AuthenticatedAsUser != null)
            {
                localSession.IsAuthenticated = true;
                localSession.AuthenticatedUser = providerResult.AuthenticatedAsUser;
            }
            else
            {
                localSession.FailedMessage = providerResult.FailedMessage;
            }
            StoreSession(localSession, currentUser);
            return (Session: localSession, WasAuthenticated: providerResult.AuthenticatedAsUser != null);
        }

        public CommonElectronicAuthenticationSession CreateSession(ICivicRegNumber civicRegNumber,
            NtechCurrentUserMetadata currentUser,
            Dictionary<string, string> customData,
            string providerName,
            Func<CommonElectronicAuthenticationSession, (string ProviderSessionId, string BeginLoginRedirectUrl)> createProviderSession)
        {
            var localSession = new CommonElectronicAuthenticationSession
            {
                LocalSessionId = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(),
                ExpectedCivicRegNumber = civicRegNumber.NormalizedValue,
                CustomData = customData,
                ProviderName = providerName
            };

            var result = createProviderSession(localSession);

            localSession.ProviderSessionId = result.ProviderSessionId;
            localSession.BeginLoginRedirectUrl = result.BeginLoginRedirectUrl;

            StoreSession(localSession, currentUser);

            return localSession;
        }

        public void StoreSession(CommonElectronicAuthenticationSession session, NtechCurrentUserMetadata user)
        {
            sessionStore.StoreSession(session, TimeSpan.FromDays(7), user.CoreUser);
        }

        public void ArchiveOldSessions()
        {
            sessionStore.ArchiveOldSessions();
        }

        public CommonElectronicAuthenticationSession GetSession(string localSessionId) => sessionStore.GetSession(localSessionId);
    }
}