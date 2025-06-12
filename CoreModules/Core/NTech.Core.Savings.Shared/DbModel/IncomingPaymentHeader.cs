using System;
using System.Collections.Generic;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel
{
    public class IncomingPaymentHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public bool IsFullyPlaced { get; set; }
        public IncomingPaymentFileHeader IncomingPaymentFile { get; set; }
        public int? IncomingPaymentFileId { get; set; }
        public virtual List<LedgerAccountTransaction> Transactions { get; set; }
        public virtual List<IncomingPaymentHeaderItem> Items { get; set; }
    }
}