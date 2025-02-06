using NTech.Core.Module.Shared.Database;

namespace nPreCredit.DbModel
{
    public class KeyValueItem : InfrastructureBaseItem
    {
        public string Key { get; set; }
        public string KeySpace { get; set; }
        public string Value { get; set; }
    }
}
namespace nPreCredit
{
    public enum KeyValueStoreKeySpaceCode
    {
        DirectDebitStatusState,
        UcbvValuationV1,
        ExternalApplicationRequestJson,
        OutgoingSettlementPaymentV2,
        MortgageLoanCurrentLoansV1,
        MortgageLoanAmortizationModelV1,
        HouseholdIncomeModelV1,
        CompanyLoanSignatureSessionV1,
        MortgageLoanObjectV1,
        MortgageLoanOfferV1,
        LockedAgreementV1,
        CustomerCheckPointMigrationStatusV1,
        WebApplicationPreScoreResult
    }
}