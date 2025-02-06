namespace nCreditReport
{
    public class EncryptedCreditReportItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditReportHeader CreditReport { get; set; }
        public int CreditReportHeaderId { get; set; }
        public string Name { get; set; }
        public byte[] Value { get; set; }
    }
}