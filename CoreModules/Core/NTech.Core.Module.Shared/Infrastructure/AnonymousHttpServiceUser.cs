using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public class AnonymousHttpServiceUser : INHttpServiceUser
    {
        public static INHttpServiceUser SharedInstance { get; } = new AnonymousHttpServiceUser();

        public string GetBearerToken() => null;
        public Task<string> GetBearerTokenAsync() => Task.FromResult((string)null);
    }
}
