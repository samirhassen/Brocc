using NTech.Core.Module.Shared.Database;

namespace nSavings
{
    public class SavingsAccountWithdrawalAccountChange : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string PowerOfAttorneyDocumentArchiveKey { get; set; } //sv: fullmakt
        public string NewWithdrawalIban { get; set; }
        public BusinessEvent InitiatedByEvent { get; set; }
        public int InitiatedByBusinessEventId { get; set; }
        public BusinessEvent CommitedOrCancelledByEvent { get; set; }
        public int? CommitedOrCancelledByEventId { get; set; }
    }
}