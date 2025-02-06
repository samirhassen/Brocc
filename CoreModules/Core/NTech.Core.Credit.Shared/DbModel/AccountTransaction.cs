using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public enum TransactionAccountType
    {
        CapitalDebt, //Current capital debt
        NotNotifiedCapital, //Should be notified
        ShouldBePaidToCustomer, //Should be paid out to the customer
        InterestDebt,
        NotificationFeeDebt,
        ReminderFeeDebt,
        UnplacedPayment,
        CapitalizedInterest,
        CapitalizedNotificationFee,
        InitialFeeDrawnFromLoanAmount,
        SwedishRseDebt,
        NotNotifiedNotificationCost,
        NotificationCost
    }

    public class AccountTransaction : InfrastructureBaseItem
    {
        public long Id { get; set; }
        public string AccountCode { get; set; }
        public int? CreditNotificationId { get; set; }
        public CreditNotificationHeader CreditNotification { get; set; }
        public int BusinessEventId { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public int? IncomingPaymentId { get; set; }
        public IncomingPaymentHeader IncomingPayment { get; set; }
        public CreditReminderHeader Reminder { get; set; }
        public int? ReminderId { get; set; }
        public WriteoffHeader Writeoff { get; set; }
        public int? OutgoingPaymentId { get; set; }
        public OutgoingPaymentHeader OutgoingPayment { get; set; }
        public int? WriteoffId { get; set; }
        public OutgoingBookkeepingFileHeader OutgoingBookkeepingFile { get; set; }
        public int? OutgoingBookkeepingFileHeaderId { get; set; }
        public CreditPaymentFreeMonth PaymentFreeMonth { get; set; }
        public int? CreditPaymentFreeMonthId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }

        /// <summary>
        /// Code that indicates this transactions role in a business event.
        /// For example when capitalizing interest there will be two capital transactions where one is the added total amount
        /// and the other is the withheld interest.
        /// This is for grouping and presentation only. Do not use for critical logic.
        /// </summary>
        public string BusinessEventRoleCode { get; set; }

        /// <summary>
        /// Subtype of account code used to customize some parts of the system without changing fundamental batching logic.
        ///
        /// Like splitting up initial fees into multiple types for notification or bookkeeping while still allowing
        /// "total initial fee" to just be a sum of AccountCode = InitialFee
        /// </summary>
        public string SubAccountCode { get; set; }
    }
}