using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class AuditClient : IAuditClient
    {
        private ServiceClient client;
        public AuditClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nAudit");
        }

        public async Task CreateSystemLogBatchAsync(List<AuditClientSystemLogItem> items)
        {
            await client.CallVoid(
                x => x.PostJson("Api/SystemLog/Create-Batch", new { items }),
                x => x.EnsureSuccessStatusCode(),
                timeout: TimeSpan.FromSeconds(60),
                isCoreHosted: true);
        }

        public void CreateSystemLogBatch(List<AuditClientSystemLogItem> items)
        {
            client.ToSync<object>(async () =>
            {
                await CreateSystemLogBatchAsync(items);
                return null;
            });
        }

        public async Task LogTelemetryDataAsync(string datasetName, string batchAsJson) =>
            await client.CallVoid(
                x => x.PostJson("Api/Telemetry/LogData", new { datasetName, batchAsJson }),
                x => x.EnsureSuccessStatusCode(), isCoreHosted: true);

        public void LogTelemetryData(string datasetName, string batchAsJson) => 
            client.ToSync(() => LogTelemetryDataAsync(datasetName, batchAsJson));
    }
}
