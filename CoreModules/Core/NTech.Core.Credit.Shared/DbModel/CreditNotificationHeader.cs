using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class CreditNotificationHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public DateTime NotificationDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ClosedTransactionDate { get; set; }
        public string OcrPaymentReference { get; set; }
        public string PdfArchiveKey { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public bool? IsCoNotificationMaster { get; set; }
        public string CoNotificationId { get; set; }
        public OutgoingCreditNotificationDeliveryFileHeader DeliveryFile { get; set; }
        public int? OutgoingCreditNotificationDeliveryFileHeaderId { get; set; }
        public virtual List<AccountTransaction> Transactions { get; set; }
        public virtual List<CreditReminderHeader> Reminders { get; set; }
    }
}