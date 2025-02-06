using System;

namespace nPreCredit.DbModel
{
    public class AffiliateReportingLogItem
    {
        public int Id { get; set; }
        public long IncomingApplicationEventId { get; set; }
        public string MessageText { get; set; }
        public string ExceptionText { get; set; }
        public string ProcessedStatus { get; set; }
        public string OutgoingRequestBody { get; set; }
        public string OutgoingResponseBody { get; set; }
        public string ProviderName { get; set; }
        public string ThrottlingContext { get; set; }
        public int? ThrottlingCount { get; set; }
        public DateTime LogDate { get; set; }
        public DateTime DeleteAfterDate { get; set; }
    }
}