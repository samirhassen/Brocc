namespace nCustomer.DbModel
{
    public class CustomerIdSequence
    {
        public int CustomerId { get; set; }
        public string CivicRegNrHash { get; set; }
        public byte[] Timestamp { get; set; } //To support replication
    }
}