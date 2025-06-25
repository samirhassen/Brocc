using System;
using System.Collections.Generic;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel
{
    public class OutgoingPaymentFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExternalId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public virtual List<OutgoingPaymentHeader> Payments { get; set; }
    }
}