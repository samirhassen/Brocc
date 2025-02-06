namespace nPreCredit.Code.AffiliateReporting
{

    public class ApplicationStateModel
    {
        public string creditCheckStatus { get; set; }
        public string providerApplicationId { get; set; }
        public string providerName { get; set; }
        public bool isCreditDecisionAccepted { get; set; }
        public CreditDecisionModelParser.AcceptedNewCreditOffer offer { get; set; }
        public string applicationWrapperUrl { get; set; }
        public string[] rejectionReasons { get; set; }
        public string afterCreditCheckRejectionReason { get; set; }
        public AdditionalLoanOfferModel additionalLoanOffer { get; set; }

        public class AdditionalLoanOfferModel
        {
            public decimal? amount { get; set; }
            public string creditNr { get; set; }
            public decimal? newAnnuityAmount { get; set; }
            public decimal? newMarginInterestRatePercent { get; set; }
            public AdditionalLoanStateAfterModel loanStateAfter { get; set; }
        }

        public class AdditionalLoanStateAfterModel
        {
            public decimal balance { get; set; }
            public int repaymentTimeInMonths { get; set; }
            public decimal annuityAmount { get; set; }
            public decimal notificationFeeAmount { get; set; }
            public decimal marginInterestRatePercent { get; set; }
            public decimal? referenceInterestRatePercent { get; set; }
            public decimal? effectiveInterestRatePercent { get; set; }
        }
    }
}