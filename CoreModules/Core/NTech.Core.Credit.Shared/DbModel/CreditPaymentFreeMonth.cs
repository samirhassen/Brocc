using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class CreditPaymentFreeMonth : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime NotificationDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public virtual List<AccountTransaction> Transactions { get; set; }
    }
}