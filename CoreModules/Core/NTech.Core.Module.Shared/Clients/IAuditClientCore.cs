using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public interface IAuditClient
    {
        Task CreateSystemLogBatchAsync(List<AuditClientSystemLogItem> items);
        Task LogTelemetryDataAsync(string datasetName, string batchAsJson);
        void LogTelemetryData(string datasetName, string batchAsJson);
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
