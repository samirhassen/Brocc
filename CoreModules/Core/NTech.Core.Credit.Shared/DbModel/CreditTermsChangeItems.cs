using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditTermsChangeItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByEventId { get; set; }
        public int CreditTermsChangeHeaderId { get; set; }
        public CreditTermsChangeHeader CreditTermsChange { get; set; }
        public string Name { get; set; }
        public int? ApplicantNr { get; set; }
        public string Value { get; set; }

        public enum CreditTermsChangeItemCode
        {
            NewMarginInterestRatePercent,
            NewInterestBoundFromDate,
            NewReferenceInterestRatePercent,
            NewRepaymentTimeInMonths,
            UnsignedAgreementDocumentArchiveKey,
            SignedAgreementDocumentArchiveKey,
            SignatureSessionKey,
            SignatureCallbackToken,
            SignatureProviderName,
            NewInterestRebindMonthCount,
            MlScheduledDate
        }
    }
}