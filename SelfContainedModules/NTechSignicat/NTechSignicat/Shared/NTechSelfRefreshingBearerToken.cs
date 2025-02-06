using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public class NTechSelfRefreshingBearerToken
    {
        public NTechSelfRefreshingBearerToken(Func<Task<Tuple<string, DateTimeOffset>>> getNewTokenAndRefreshDate)
        {
            this.getNewTokenAndRefreshDate = getNewTokenAndRefreshDate;
        }

        private readonly Func<Task<Tuple<string, DateTimeOffset>>> getNewTokenAndRefreshDate;
        private string currentToken;
        private DateTimeOffset nextRefreshDate;

        public async Task<string> GetToken()
        {
            if (currentToken == null || DateTimeOffset.UtcNow >= nextRefreshDate)
            {
                if (currentToken == null || DateTimeOffset.UtcNow >= nextRefreshDate)
                {
                    var t = await getNewTokenAndRefreshDate();
                    if (t.Item1 == null)
                        throw new Exception("Could not aquire new token");
                    nextRefreshDate = t.Item2; //Set to halfway between
                    currentToken = t.Item1;
                }
            }
            return currentToken;
        }

        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(NTechServiceRegistry serviceRegistry, string username, string password)
        {
            return new NTechSelfRefreshingBearerToken(async () =>
            {
                var tokenClient = new TokenClient(
                    serviceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                    "nTechSystemUser",
                    "nTechSystemUser");
                var token = await tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: "nTech1");

                if (token.IsError)
                {
                    throw new Exception("Login error: " + token.Error);
                }
                else
                {
                    long refreshInSeconds;
                    if (token.ExpiresIn <= 0)
                        refreshInSeconds = 300;//Should never happen but be conservative in this case and refresh often
                    else
                        refreshInSeconds = (long)Math.Ceiling(((double)token.ExpiresIn) / 2.0d); //Wait until halfway to expiration then refresh

                    return Tuple.Create(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(refreshInSeconds));
                }
            });
        }
    }
}
