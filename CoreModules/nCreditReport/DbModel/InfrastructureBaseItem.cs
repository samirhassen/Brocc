using System;

namespace nCreditReport
{
    public class InfrastructureBaseItem
    {
        public byte[] Timestamp { get; set; } //To support replication
        public int ChangedById { get; set; }
        public DateTimeOffset ChangedDate { get; set; }
        public string InformationMetaData { get; set; }
    }
}