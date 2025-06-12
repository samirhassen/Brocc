using System;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel
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