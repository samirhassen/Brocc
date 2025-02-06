namespace nPreCredit
{
    public class PricingResult
    {
        public decimal? MaxLoanAmount { get; set; }
        public decimal? SuggestedLoanAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? InitialFee { get; set; }
        public decimal? NotificationFee { get; set; }
        public int? SuggestedRepaymentTimeInMonths { get; set; }
    }
}
