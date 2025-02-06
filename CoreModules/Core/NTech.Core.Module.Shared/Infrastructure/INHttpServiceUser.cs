using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public interface INHttpServiceUser
    {
        string GetBearerToken();
        Task<string> GetBearerTokenAsync();
    }
}