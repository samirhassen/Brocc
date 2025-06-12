using System;
using IdentityModel.Client;

namespace NTech.Services.Infrastructure
{
    public class NTechSelfRefreshingBearerToken
    {
        public NTechSelfRefreshingBearerToken(Func<Tuple<string, DateTimeOffset>> getNewTokenAndRefreshDate)
        {
            this.getNewTokenAndRefreshDate = getNewTokenAndRefreshDate;
        }

        private readonly Func<Tuple<string, DateTimeOffset>> getNewTokenAndRefreshDate;
        private string currentToken;
        private DateTimeOffset nextRefreshDate;

        public string GetToken()
        {
            if (currentToken != null && DateTimeOffset.UtcNow < nextRefreshDate) return currentToken;
            if (currentToken != null && DateTimeOffset.UtcNow < nextRefreshDate) return currentToken;
            var t = getNewTokenAndRefreshDate();
            currentToken = t.Item1 ?? throw new Exception("Could not acquire new token");
            nextRefreshDate = t.Item2; //Set to halfway between
            return currentToken;
        }

        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(
            NTechServiceRegistry serviceRegistry, Tuple<string, string> usernameAndPassword)
        {
            return CreateSystemUserBearerTokenWithUsernameAndPassword(serviceRegistry, usernameAndPassword.Item1,
                usernameAndPassword.Item2);
        }

        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(
            NTechServiceRegistry serviceRegistry, string username, string password)
        {
            return new NTechSelfRefreshingBearerToken(() =>
            {
                var tokenClient = new TokenClient(
                    serviceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                    "nTechSystemUser",
                    "nTechSystemUser");
                var token = tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: "nTech1").Result;

                if (token.IsError) throw new Exception($"Login error: {token.Error}");

                long refreshInSeconds;
                if (token.ExpiresIn <= 0)
                    refreshInSeconds = 300; //Should never happen but be conservative in this case and refresh often
                else
                    refreshInSeconds =
                        (long)Math.Ceiling(((double)token.ExpiresIn) /
                                           2.0d); //Wait until halfway to expiration then refresh

                return Tuple.Create(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(refreshInSeconds));
            });
        }
    }
}