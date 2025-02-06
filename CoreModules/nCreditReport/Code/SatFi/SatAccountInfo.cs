using System;

namespace nCreditReport.Code.SatFi
{
    public class SatAccountInfo
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string HashKey { get; set; }
        public string EndpointUrl { get; set; }
        public TimeSpan? ClockDrag { get; set; }
        public string OverrideTarget { get; set; }
    }
}