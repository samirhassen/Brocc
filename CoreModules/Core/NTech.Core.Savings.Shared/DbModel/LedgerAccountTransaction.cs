using System;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.DbModel
{
    public enum LedgerAccountTypeCode
    {
        UnplacedPayment,
        Capital,
        CapitalizedInterest,
        ShouldBePaidToCustomer,
        WithheldCapitalizedInterestTax
    }

    public class LedgerAccountTransaction : InfrastructureBaseItem
    {
        public long Id { get; set; }
        public string AccountCode { get; set; }
        public int BusinessEventId { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public string SavingsAccountNr { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public int? IncomingPaymentId { get; set; }
        public IncomingPaymentHeader IncomingPayment { get; set; }
        public int? OutgoingPaymentId { get; set; }
        public OutgoingPaymentHeader OutgoingPayment { get; set; }
        public OutgoingBookkeepingFileHeader OutgoingBookkeepingFile { get; set; }
        public int? OutgoingBookkeepingFileHeaderId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public DateTime InterestFromDate { get; set; }
        /// <summary>
        /// Code that indicates this transactions role in a business event.
        /// For example when capitalizing interest there will be two capital transactions where one is the added total amount
        /// and the other is the withheld interest.
        /// This is for grouping and presentation only. Do not use for critical logic.
        /// </summary>
        public string BusinessEventRoleCode { get; set; }
    }
}