using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditOutgoingDirectDebitItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public int CreatedByEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public string Operation { get; set; }
        public int? BankAccountOwnerCustomerId { get; set; }
        public string BankAccountNr { get; set; }
        public string ClientBankGiroNr { get; set; }
        public string PaymentNr { get; set; }
        public OutgoingDirectDebitStatusChangeFileHeader OutgoingDirectDebitStatusChangeFile { get; set; }
        public int? OutgoingDirectDebitStatusChangeFileHeaderId { get; set; }
    }
}