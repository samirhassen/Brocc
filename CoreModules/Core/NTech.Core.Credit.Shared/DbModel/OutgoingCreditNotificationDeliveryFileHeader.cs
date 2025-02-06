using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class OutgoingCreditNotificationDeliveryFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExternalId { get; set; }
        public virtual List<CreditNotificationHeader> Notifications { get; set; }
        public int? BusinessEvent_Id { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
    }
}