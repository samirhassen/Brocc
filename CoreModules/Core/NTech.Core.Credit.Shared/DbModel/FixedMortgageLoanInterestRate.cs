namespace nCredit
{
    public class FixedMortgageLoanInterestRate : FixedMortgageLoanInterestRateBase
    {
        public int CreatedByBusinessEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
    }

    public class FixedMortgageLoanInterestRateBase
    {
        public int MonthCount { get; set; }
        public decimal RatePercent { get; set; }
    }
}