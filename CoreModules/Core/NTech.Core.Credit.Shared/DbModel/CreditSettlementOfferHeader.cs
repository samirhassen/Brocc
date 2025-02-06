using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class CreditSettlementOfferHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public DateTime ExpectedSettlementDate { get; set; }
        public DateTime? AutoExpireDate { get; set; }
        public CreditHeader Credit { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public BusinessEvent CommitedByEvent { get; set; }
        public BusinessEvent CancelledByEvent { get; set; }
        public int CreatedByEventId { get; set; }
        public int? CommitedByEventId { get; set; }
        public int? CancelledByEventId { get; set; }
        public virtual List<CreditSettlementOfferItem> Items { get; set; }
    }
}
