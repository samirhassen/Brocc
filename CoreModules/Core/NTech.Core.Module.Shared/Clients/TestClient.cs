using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class TestClient : ITestClient
    {
        private ServiceClient client;
        public TestClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nTest");
        }

        public async Task<DateTimeOffset?> GetCurrentTimeAsync() => (await client.Call(
                x => x.PostJson("Api/TimeMachine/GetCurrentTime", new { }),
                x => x.ParseJsonAsAnonymousType(new { currentTime = (DateTimeOffset?)null })))?.currentTime;

        public DateTimeOffset? GetCurrentTime() => client.ToSync(() => GetCurrentTimeAsync());
    }
}
