namespace nCredit
{
    public class HFixedMortgageLoanInterestRate
    {
        public int Id { get; set; }
        public int MonthCount { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public decimal RatePercent { get; set; }
    }
}