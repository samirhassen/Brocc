namespace nPreCredit.Code
{
    public class AdditionalLoanOfferAcceptModel
    {
        public string AdditionalLoanCreditNr { get; set; }
        public decimal? AdditionalLoanAmount { get; set; }
        public decimal? NewMarginInterestRatePercent { get; set; }
        public decimal? NewAnnuityAmount { get; set; }
        public decimal? NewNotificationFeeAmount { get; set; }
    }
}