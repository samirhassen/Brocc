using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nSavings
{
    public class OutgoingBookkeepingFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime FromTransactionDate { get; set; }
        public DateTime ToTransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string XlsFileArchiveKey { get; set; }
        public virtual List<LedgerAccountTransaction> Transactions { get; set; }
    }
}