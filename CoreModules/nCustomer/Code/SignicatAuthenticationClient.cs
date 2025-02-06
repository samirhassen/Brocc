using System;
using System.Collections.Generic;

namespace nCustomer.Code
{
    public class SignicatAuthenticationClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "NTechSignicat";

        public LoginSession StartLoginSession(StartLoginSessionRequest request)
        {
            return Begin().PostJson("api/start-login-session", request).ParseJsonAs<LoginSession>();
        }

        public LoginSession GetLoginSession(GetLoginSessionRequest request)
        {
            return Begin().PostJson("api/get-login-session", request).ParseJsonAs<LoginSession>();
        }

        public LoginSession CompleteLoginSession(CompleteLoginSessionRequest request)
        {
            return Begin().PostJson("api/complete-login-session", request).ParseJsonAs<LoginSession>();
        }

        public bool TryCompleteLoginSession(CompleteLoginSessionRequest request, out LoginSession session, out string failedMessage)
        {
            var r = Begin().PostJson("api/complete-login-session", request);
            if (r.StatusCode == 400)
            {
                failedMessage = r.ReasonPhrase;
                session = null;
                return false;
            }
            r.EnsureSuccessStatusCode();

            session = r.ParseJsonAs<LoginSession>();
            failedMessage = null;

            return true;
        }

        public string GetElectronicIdLoginMethod()
        {
            var countryIsoCode = NEnv.ClientCfg.Country.BaseCountry;
            if (countryIsoCode == "FI")
                return "FinnishTrustNetwork";
            else if (countryIsoCode == "SE")
                return "SwedishBankId";
            else
                throw new NotImplementedException();
        }

        public class CompleteLoginSessionRequest
        {
            public string SessionId { get; set; }
            public string Token { get; set; }
        }

        public class GetLoginSessionRequest
        {
            public string SessionId { get; set; }
        }

        public class StartLoginSessionRequest
        {
            public string ExpectedCivicRegNr { get; set; }

            public List<string> LoginMethods { get; set; }

            public string RedirectAfterSuccessUrl { get; set; }

            public string RedirectAfterFailedUrl { get; set; }

            public Dictionary<string, string> CustomData { get; set; }
        }

        public class LoginSession
        {
            public string ExpectedCivicRegNr { get; set; }
            public string ExpectedCivicRegNrCountryIsoCode { get; set; }
            public string Id { get; set; }
            public string SessionStateCode { get; set; }
            public DateTime ExpirationDateUtc { get; set; }
            public DateTime StartDateUtc { get; set; }
            public DateTime? CallbackDateUtc { get; set; }
            public DateTime? LoginDateUtc { get; set; }
            public string SignicatReturnUrl { get; set; }
            public string SignicatInitialUrl { get; set; }
            public TokenSetModel Tokens { get; set; }
            public UserInfoModel UserInfo { get; set; }
            public string FailedCode { get; set; }
            public string FailedMessage { get; set; }
            public string RedirectAfterSuccessUrl { get; set; }
            public string RedirectAfterFailedUrl { get; set; }
            public string OneTimeInternalLoginToken { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }

        public class TokenSetModel
        {
            public string AccessToken { get; set; }
            public string IdToken { get; set; }
            public DateTime? ExpiresDateUtc { get; set; }
            public ISet<string> Scopes { get; set; }
        }

        public class UserInfoModel
        {
            public string CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}