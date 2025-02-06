using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class OutgoingDirectDebitStatusChangeFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExternalId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByEventId { get; set; }
        public virtual List<CreditOutgoingDirectDebitItem> CreditOutgoingDirectDebitItems { get; set; }
    }
}