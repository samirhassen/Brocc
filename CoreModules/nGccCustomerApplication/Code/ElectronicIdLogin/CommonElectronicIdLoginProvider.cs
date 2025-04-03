using nGccCustomerApplication.Code;
using System;
using System.Collections.Generic;

namespace nGccCustomerApplication.Code.ElectronicIdLogin
{
    public class CommonElectronicIdLoginProvider
    {
        public CommonElectronicIdLoginProvider()
        {

        }
        public const string AuthTypeNameShared = "NtechGccCustomerApplicationCommonEid";

        public string AuthTypeName => AuthTypeNameShared;

        public bool IsEnabled => IsProviderEnabled;

        public static bool IsProviderHandled(Code.ElectronicIdSignature.ElectronicIdProviderCode providerCode)
        {
            return providerCode == Code.ElectronicIdSignature.ElectronicIdProviderCode.Scrive
                || providerCode == Code.ElectronicIdSignature.ElectronicIdProviderCode.Mock
                || providerCode == Code.ElectronicIdSignature.ElectronicIdProviderCode.Signicat;
        }

        public static bool IsProviderEnabled =>
            NEnv.IsDirectEidAuthenticationModeEnabled || NEnv.IsEmbeddedSiteEidLoginApiEnabled;

        public ElectronicIdLoginResult GetLoginSessionResult(Dictionary<string, string> providerParameters)
        {
            var localSessionId = providerParameters?.Opt("localSessionId");
            if (localSessionId == null)
                return new ElectronicIdLoginResult { IsSuccess = false };

            var client = new Clients.SystemUserCustomerClient();

            var result = client.HandleElectronicIdAuthenticationProviderEvent(localSessionId, providerParameters);
            if (!result.WasAuthenticated)
                return new ElectronicIdLoginResult { IsSuccess = false };

            var session = result.Session;

            return new ElectronicIdLoginResult
            {
                IsSuccess = true,
                AdditionalData = session.CustomData.Opt("additionalData"),
                CivicNr = NEnv.BaseCivicRegNumberParser.Parse(session.AuthenticatedUser.CivicRegNumber).NormalizedValue,
                TargetName = session.CustomData.Opt("targetName")
            };
        }

        public string StartLoginSessionReturningLoginUrl(string civicRegNumber, string targetName, string additionalData)
        {
            var standardReturnUrl = NEnv.ServiceRegistry.External.ServiceUrl(
                "nGccCustomerApplication",
                "login/eid/{localSessionId}/return").ToString();

            var client = new Clients.SystemUserCustomerClient();
            var session = client.CreateElectronicIdAuthenticationSession(civicRegNumber, new Dictionary<string, string>
            {
                { "additionalData", additionalData },
                { "targetName", targetName },
            }, standardReturnUrl);
            return session.BeginLoginRedirectUrl;
        }
    }
}