using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nSavings
{
    public class OutgoingPaymentHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string UniqueToken { get; set; } //Used to prevent duplicate events. Is nullable but must be unique when set
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public int? OutgoingPaymentFileHeaderId { get; set; }
        public OutgoingPaymentFileHeader OutgoingPaymentFile { get; set; }
        public virtual List<LedgerAccountTransaction> Transactions { get; set; }
        public virtual List<OutgoingPaymentHeaderItem> Items { get; set; }
    }
}