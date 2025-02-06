using System;

namespace nAudit
{
    public class SystemLogItem
    {
        public int Id { get; set; }
        public string Level { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public string EventType { get; set; }
        public string ServiceName { get; set; }
        public string ServiceVersion { get; set; }
        public string RequestUri { get; set; }
        public string RemoteIp { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionData { get; set; }
    }
}