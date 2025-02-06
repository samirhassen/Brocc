using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class IncomingPaymentHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public bool IsFullyPlaced { get; set; }
        public IncomingPaymentFileHeader IncomingPaymentFile { get; set; }
        public int? IncomingPaymentFileId { get; set; }
        public virtual List<AccountTransaction> Transactions { get; set; }
        public virtual List<IncomingPaymentHeaderItem> Items { get; set; }
    }
}