using Duende.IdentityModel.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
            if (currentToken == null || DateTimeOffset.UtcNow >= nextRefreshDate)
            {
                if (currentToken == null || DateTimeOffset.UtcNow >= nextRefreshDate)
                {
                    var t = getNewTokenAndRefreshDate();
                    if (t.Item1 == null)
                        throw new Exception("Could not aquire new token");
                    nextRefreshDate = t.Item2; //Set to halfway between
                    currentToken = t.Item1;
                }
            }
            return currentToken;
        }

        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(NTechServiceRegistry serviceRegistry, Tuple<string, string> usernameAndPassword)
        {
            return CreateSystemUserBearerTokenWithUsernameAndPassword(serviceRegistry, usernameAndPassword.Item1, usernameAndPassword.Item2);
        }

        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(NTechServiceRegistry serviceRegistry, string username, string password)
        {
            return new NTechSelfRefreshingBearerToken(() =>
            {                
                var client = new HttpClient();                
                var token = client.RequestPasswordTokenAsync(new PasswordTokenRequest()
                {
                    Address = serviceRegistry.Internal.ServiceUrl("nUser", "id/connect/token").ToString(),
                    ClientId = "nTechSystemUser",
                    ClientSecret = "nTechSystemUser",
                    UserName = username,
                    Password=password,
                    Scope= "nTech1"
                });

                if (token.Result.IsError)
                {
                    throw new Exception("Login error: " + token.Result.Error);
                }
                else
                {
                    long refreshInSeconds;
                    if (token.Result.ExpiresIn <= 0)
                        refreshInSeconds = 300;//Should never happen but be conservative in this case and refresh often
                    else
                        refreshInSeconds = (long)Math.Ceiling(((double)token.Result.ExpiresIn) / 2.0d); //Wait until halfway to expiration then refresh

                    return Tuple.Create(token.Result.AccessToken, DateTimeOffset.UtcNow.AddSeconds(refreshInSeconds));
                }
            });
        }
    }
}