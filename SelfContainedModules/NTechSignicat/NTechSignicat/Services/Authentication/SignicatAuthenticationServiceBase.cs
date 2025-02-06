using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NTech.Banking.CivicRegNumbers;
using NTech.Shared.Randomization;

namespace NTechSignicat.Services
{
    public abstract class SignicatAuthenticationServiceBase<TLogger> : ISignicatAuthenticationService
    {
        protected readonly SignicatSettings settings;
        protected readonly ILogger<TLogger> logger;
        private readonly IDocumentDatabaseService documentDatabaseService;
        private Uri redirectUrl;

        private const string DocumentDbSessionKeySpace = "LoginSessionsV1";

        public SignicatAuthenticationServiceBase(SignicatSettings settings, ILogger<TLogger> logger, IDocumentDatabaseService documentDatabaseService)
        {
            this.settings = settings;
            this.logger = logger;
            this.documentDatabaseService = documentDatabaseService;
            this.redirectUrl = UrlBuilder.Create(settings.SelfExternalUrl, "redirect").ToUri();
        }

        protected abstract Task<Uri> GetCustomerLoginUrl(ICivicRegNumber preFilledCivicRegNr, List<SignicatLoginMethodCode> loginMethods, string sessionId, bool requestNationalId);

        protected abstract Task<TokenSetModel> GetToken(string code, LoginSession session);

        protected abstract Task<UserInfoModel> GetUserInfo(string accessToken, LoginSession session);

        public LoginSession CompleteInternalLogin(string sessionId, string loginToken)
        {
            LoginSession session = GetLoginSession(sessionId);

            if (session == null)
            {
                return null;
            }
            var state = session.GetState();

            Func<string, string, LoginSession> fail = (c, msg) =>
            {
                session.FailedCode = c;
                session.FailedMessage = msg;
                session.SetState(LoginSessionStateCode.Failed);
                return UpdateSession(session);
            };

            if (state != LoginSessionStateCode.PendingLogin)
            {
                return fail("completeInternalLoginFromInvalidState",
                  $"complete login attempted in state {state.ToString()} instead of {LoginSessionStateCode.PendingLogin.ToString()}");
            }

            if (loginToken != session.OneTimeInternalLoginToken)
            {
                return fail("invalidLoginToken", "Invalid login token");
            }

            session.SetState(LoginSessionStateCode.LoginSuccessful);

            return session;
        }

        public LoginSession ReceiveSignicatErrorCallback(string sessionId, string errorCode, string errorMessage)
        {
            LoginSession session = GetLoginSession(sessionId);

            if (session == null)
            {
                return null;
            }
            var state = session.GetState();

            Func<string, string, LoginSession> fail = (c, msg) =>
            {
                session.FailedCode = c;
                session.FailedMessage = msg;
                session.SetState(LoginSessionStateCode.Failed);
                return UpdateSession(session);
            };

            if (session.GetState() != LoginSessionStateCode.PendingCallback)
            {
                return fail("receiveCallbackFromInvalidState",
                    $"Callback received in state {state.ToString()} instead of {LoginSessionStateCode.PendingCallback.ToString()}");
            }

            return fail($"signicat_{errorCode}", errorMessage);
        }

        public async Task<LoginSession> ReceiveSignicatSuccessCallback(string sessionId, string code)
        {
            LoginSession session = GetLoginSession(sessionId);

            if (session == null)
            {
                return null;
            }
            var state = session.GetState();

            Func<string, string, LoginSession> fail = (c, msg) =>
            {
                session.FailedCode = c;
                session.FailedMessage = msg;
                session.SetState(LoginSessionStateCode.Failed);
                return UpdateSession(session);
            };

            if (session.GetState() != LoginSessionStateCode.PendingCallback)
            {
                return fail("receiveCallbackFromInvalidState",
                    $"Callback received in state {state.ToString()} instead of {LoginSessionStateCode.PendingCallback.ToString()}");
            }

            if (session.ExpirationDateUtc < DateTime.UtcNow)
            {
                return fail("sessionExpired", "The session expired");
            }

            TokenSetModel token = null;
            var tokenOk = await TryCatch(async () =>
            {
                token = await GetToken(code, session);
            }, session, "getTokenFailed", $"GetToken failed for session '{sessionId}'");

            if (!tokenOk)
                return session;

            session.Tokens = token;

            UserInfoModel userInfo = null;
            var userInfoOk = await TryCatch(async () =>
            {
                userInfo = await GetUserInfo(token.AccessToken, session);
            }, session, "getUserInfoFailed", $"GetUserInfo failed for session '{sessionId}'");

            if (!userInfoOk)
                return session;

            if (!string.IsNullOrWhiteSpace(session.ExpectedCivicRegNr) && session.ExpectedCivicRegNr != userInfo.CivicRegNr)
            {
                return fail("invalidCivicRegNr", "Expected a different person");
            }

            session.SetState(LoginSessionStateCode.PendingLogin);
            session.UserInfo = userInfo;
            session.OneTimeInternalLoginToken = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(length: 16);

            return UpdateSession(session);
        }

        protected LoginSession UpdateSession(LoginSession session)
        {
            documentDatabaseService.Set(DocumentDbSessionKeySpace, session.Id, session, TimeSpan.FromHours(1));
            return session;
        }

        public LoginSession GetLoginSession(string sessionId)
        {
            return documentDatabaseService.Get<LoginSession>(DocumentDbSessionKeySpace, sessionId);
        }

        public async Task<LoginSession> StartLoginSession(
            ICivicRegNumber expectedCivicRegNr,
            List<SignicatLoginMethodCode> loginMethods,
            Uri redirectAfterSuccessUrl,
            Uri redirectAfterFailedUrl,
            Dictionary<string, string> customData = null)
        {
            var id = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(length: 10);

            var loginCivicRegNr = expectedCivicRegNr;
            var usesTestReplacementCivicRegNr = false;
            settings.WithTestReplacementCivicRegNr(expectedCivicRegNr, 1, x =>
            {
                loginCivicRegNr = x;
                usesTestReplacementCivicRegNr = true;
            }, true);

            var customerLoginUrl = await GetCustomerLoginUrl(loginCivicRegNr, loginMethods, id, true);

            var s = new LoginSession
            {
                StartDateUtc = DateTime.UtcNow,
                ExpectedCivicRegNr = expectedCivicRegNr?.NormalizedValue,
                ExpectedCivicRegNrCountryIsoCode = expectedCivicRegNr?.Country,
                ExpirationDateUtc = DateTime.UtcNow.AddMinutes(30),
                UsesTestReplacementCivicRegNr = usesTestReplacementCivicRegNr,
                Id = id,
                SignicatReturnUrl = redirectUrl.ToString(),
                SignicatInitialUrl = customerLoginUrl.ToString(),
                RedirectAfterFailedUrl = redirectAfterFailedUrl.ToString().Replace("{{SessionId}}", id),
                RedirectAfterSuccessUrl = redirectAfterSuccessUrl.ToString().Replace("{{SessionId}}", id),
                CustomData = customData ?? new Dictionary<string, string>(),
            };
            s.SetState(LoginSessionStateCode.PendingCallback);

            documentDatabaseService.Set(DocumentDbSessionKeySpace, s.Id, s, TimeSpan.FromHours(1));

            return s;
        }

        private async Task<bool> TryCatch(Func<Task> a, LoginSession session, string failedCode, string failedMessage)
        {
            try
            {
                await a();
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, failedMessage);
                session.FailedCode = failedCode;
                session.FailedMessage = failedMessage;
                session.SetState(LoginSessionStateCode.Failed);
                UpdateSession(session);
                return false;
            }
        }
    }
}