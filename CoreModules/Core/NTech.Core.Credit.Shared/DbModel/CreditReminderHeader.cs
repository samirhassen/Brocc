using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class CreditReminderHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime ReminderDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime InternalDueDate { get; set; } //Next reminder cannot be sent until this has passed
        public DateTime BookKeepingDate { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public int NotificationId { get; set; }
        public virtual List<CreditDocument> Documents { get; set; }
        public int ReminderNumber { get; set; }
        public bool? IsCoReminderMaster { get; set; }
        public string CoReminderId { get; set; }
        public CreditNotificationHeader Notification { get; set; }
        public OutgoingCreditReminderDeliveryFileHeader DeliveryFile { get; set; }
        public int? OutgoingCreditReminderDeliveryFileHeaderId { get; set; }
        public virtual List<AccountTransaction> Transactions { get; set; }
    }
}