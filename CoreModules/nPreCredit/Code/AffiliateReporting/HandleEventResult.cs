using System;

namespace nPreCredit.Code.AffiliateReporting
{
    public class HandleEventResult
    {
        public string Message { get; set; }
        public AffiliateReportingEventResultCode Status { get; set; }
        public TimeSpan? WaitUntilNextAttempt { get; set; }
        public Exception Exception { get; set; }
        public Tuple<int, string> ThrottlingCountAndContext { get; set; }
        public string OutgoingRequestBody { get; set; }
        public string OutgoingResponseBody { get; set; }
    }
}