namespace nCreditReport
{
    public class CreditReportSearchTerm : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditReportHeader CreditReport { get; set; }
        public int CreditReportHeaderId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}