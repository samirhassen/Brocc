using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class CreditTerminationLetterHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime PrintDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public virtual List<CreditDocument> Documents { get; set; }
        public OutgoingCreditTerminationLetterDeliveryFileHeader DeliveryFile { get; set; }
        public int? OutgoingCreditTerminationLetterDeliveryFileHeaderId { get; set; }
        public int? InactivatedByBusinessEventId { get; set; }
        public BusinessEvent InactivatedByBusinessEvent { get; set; }
        public bool? SuspendsCreditProcess { get; set; }
        public bool? IsCoTerminationMaster { get; set; }
        public string CoTerminationId { get; set; }
    }
}