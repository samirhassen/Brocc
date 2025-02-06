using System;

namespace nPreCredit.DbModel
{
    public class AffiliateReportingEvent
    {
        public long Id { get; set; }
        public string ApplicationNr { get; set; }
        public string ProviderName { get; set; }
        public string EventType { get; set; }
        public string EventData { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime WaitUntilDate { get; set; }
        public DateTime DeleteAfterDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedStatus { get; set; }
    }
}