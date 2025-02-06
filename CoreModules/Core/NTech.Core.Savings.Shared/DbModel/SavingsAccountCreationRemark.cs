using NTech.Core.Module.Shared.Database;

namespace nSavings
{
    public enum SavingsAccountCreationRemarkCode
    {
        FraudCheckSameEmail,
        FraudCheckSamePhone,
        KycScreenFailed,
        FraudCheckSameWithdrawalIban,
        ContactInfoLookupIssue,
        UnknownTaxOrCitizenCountry,
        KycAttentionNeeded,
        CustomerCheckpoint
    }

    public class SavingsAccountCreationRemark : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public string RemarkCategoryCode { get; set; }
        public string RemarkData { get; set; }
    }
}