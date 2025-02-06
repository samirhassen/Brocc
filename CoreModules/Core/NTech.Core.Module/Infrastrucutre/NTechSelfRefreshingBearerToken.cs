using IdentityModel.Client;
using Nito.AsyncEx;

namespace NTech.Core.Module.Infrastrucutre
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

        public async Task<string> GetTokenAsync()
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

        public string GetToken() => AsyncContext.Run(() => GetTokenAsync());

        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(Func<System.Net.Http.HttpClient> createClient, Uri userBaseUrl, string username, string password)
        {
            return new NTechSelfRefreshingBearerToken(async () =>
            {
                var tokenClient = new TokenClient(createClient(), new TokenClientOptions
                {
                    Address = new Uri(userBaseUrl, "id/connect/token").ToString(),
                    ClientId = "nTechSystemUser",
                    ClientSecret = "nTechSystemUser"
                });
                var token = await tokenClient.RequestPasswordTokenAsync(username, password: password, scope: "nTech1");

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
                        refreshInSeconds = (long)Math.Ceiling(token.ExpiresIn / 2.0d); //Wait until halfway to expiration then refresh

                    return Tuple.Create(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(refreshInSeconds));
                }
            });
        }
        public static NTechSelfRefreshingBearerToken CreateSystemUserBearerTokenWithUsernameAndPassword(IHttpClientFactory httpClientFactory, NTechServiceRegistry serviceRegistry, string username, string password) =>
            CreateSystemUserBearerTokenWithUsernameAndPassword(() => httpClientFactory.CreateClient(), serviceRegistry.InternalServiceUrl("nUser", ""), 
                username, password);
    }
}
