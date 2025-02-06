using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class CreditFuturePaymentFreeMonth : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime ForMonth { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public BusinessEvent CancelledByEvent { get; set; }
        public int? CancelledByBusinessEventId { get; set; }
        public BusinessEvent CommitedByEvent { get; set; }
        public int? CommitedByEventBusinessEventId { get; set; }
    }
}