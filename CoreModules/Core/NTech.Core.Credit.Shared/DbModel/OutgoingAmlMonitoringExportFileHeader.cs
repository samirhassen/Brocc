using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class OutgoingAmlMonitoringExportFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExportResultStatus { get; set; }
        public string ProviderName { get; set; }
    }
}