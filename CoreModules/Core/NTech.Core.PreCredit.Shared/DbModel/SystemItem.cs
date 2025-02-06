using NTech.Core.Module.Shared.Database;

namespace nPreCredit.DbModel
{
    public enum SystemItemCode
    {
        DwLatestMergedTimestamp_Dimension_CreditApplication,
        DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot,
        DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot_ItemTs,
        DwLatestMergedTimestamp_Fact_CreditApplicationCancellation,
        DwLatestMergedTimestamp_Fact_CurrentCreditDecisionEffectiveInterestRate,
        DwLatestMergedTimestamp_Dimension_CreditApplicationArchival,
        DwLatestMergedTimestamp_Fact_CreditApplicationFinalDecision,
        DwLatestMergedTimestamp_Fact_CreditApplicationLatestCreditDecision2,
        StandardApplication_CustomerRelationMerge
    }

    public class SystemItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}