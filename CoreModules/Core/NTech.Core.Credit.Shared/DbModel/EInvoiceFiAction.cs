using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{

    public class EInvoiceFiAction : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string ActionName { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionMessage { get; set; }
        public int CreatedByUserId { get; set; }

        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }

        public EInvoiceFiMessageHeader EInvoiceFiMessage { get; set; }
        public int? EInvoiceFiMessageHeaderId { get; set; }

        public DateTime? HandledDate { get; set; } //User for error list function
        public int? HandledByUserId { get; set; }

        public int? ConnectedBusinessEventId { get; set; }
        public BusinessEvent ConnectBusinessEvent { get; set; }
    }
}