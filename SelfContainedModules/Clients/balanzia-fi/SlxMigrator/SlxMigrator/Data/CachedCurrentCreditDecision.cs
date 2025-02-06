namespace SlxMigrator
{
    internal class CachedCurrentCreditDecision
    {
        public string applicationNr { get; set; }
        public int customerId { get; set; }
        public int id { get; set; }
        public string code { get; set; }
        public decimal? amount { get; set; }
        public decimal? annuityAmount { get; set; }
        public int? repaymentTimeInMonths { get; set; }
        public decimal? marginInterestRatePercent { get; set; }
        public decimal? referenceInterestRatePercent { get; set; }
        public decimal? initialFeeAmount { get; set; }
        public decimal? notificationFeeAmount { get; set; }
        public decimal? effectiveInterestRatePercent { get; set; }
        public decimal? totalPaidAmount { get; set; }
        public decimal? initialPaidToCustomerAmount { get; set; }
        public string creditNr { get; set; }
        public decimal? newAnnuityAmount { get; set; }
        public decimal? newMarginInterestRatePercent { get; set; }
        public decimal? newNotificationFeeAmount { get; set; }
        public string rejectionReasonsJson { get; set; }
        public decimal? recommendationLeftToLiveOn { get; set; }
        public string recommendationRiskGroup { get; set; }
        public decimal? recommendationScore { get; set; }

        public decimal? getTotalInterestRatePercent() =>
            marginInterestRatePercent.HasValue || referenceInterestRatePercent.HasValue
            ? marginInterestRatePercent.GetValueOrDefault() + referenceInterestRatePercent.GetValueOrDefault()
            : new decimal?();
    }
}