using System;
using System.Collections.Generic;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel
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