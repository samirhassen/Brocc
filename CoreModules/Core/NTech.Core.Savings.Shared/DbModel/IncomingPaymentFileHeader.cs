using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nSavings
{
    public class IncomingPaymentFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExternalId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public virtual List<IncomingPaymentHeader> Payments { get; set; }
    }
}