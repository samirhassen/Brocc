using System;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public interface ITestClient
    {
        Task<DateTimeOffset?> GetCurrentTimeAsync();
        DateTimeOffset? GetCurrentTime();
    }
}
