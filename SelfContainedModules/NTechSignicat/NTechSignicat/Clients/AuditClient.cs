using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NTechSignicat.Clients
{
    public class AuditClient : NHttpServiceBase, IAuditClient
    {
        private readonly Lazy<NTechSelfRefreshingBearerToken> systemUserToken;
        private readonly IHttpClientFactory httpClientFactory;

        public AuditClient(IHttpClientFactory httpClientFactory, NTechServiceRegistry serviceRegistry, IServiceProvider serviceProvider, INEnv nEnv) : base(httpClientFactory, serviceRegistry, serviceProvider)
        {
            this.systemUserToken = new Lazy<NTechSelfRefreshingBearerToken>(() => CreateSystemUserBearerTokenWithUsernameAndPassword(
                   nEnv.RequiredSetting("ntech.automationuser.username"),
                   nEnv.RequiredSetting("ntech.automationuser.password")));
            this.httpClientFactory = httpClientFactory;
        }

        public override string ServiceName => "NTechHost";

        public async Task CreateSystemLogBatch(List<AuditClientSystemLogItem> items)
        {
            await CallVoid(
                x => x.PostJson("Api/SystemLog/Create-Batch", new { items }),
                x => x.EnsureSuccessStatusCode(),
                timeout: TimeSpan.FromSeconds(60));
        }

        protected override async Task<NHttpCall> Begin(TimeSpan? timeout = null)
        {
            return this.Begin(await systemUserToken.Value.GetToken(), timeout: timeout);
        }
    }

    public interface IAuditClient
    {
        Task CreateSystemLogBatch(List<AuditClientSystemLogItem> items);
    }

    public class AuditClientSystemLogItem
    {
        public DateTimeOffset EventDate { get; set; }
        public string Level { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}
