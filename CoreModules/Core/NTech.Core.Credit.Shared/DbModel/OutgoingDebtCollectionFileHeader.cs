using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class OutgoingDebtCollectionFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string XlsFileArchiveKey { get; set; }
    }
}