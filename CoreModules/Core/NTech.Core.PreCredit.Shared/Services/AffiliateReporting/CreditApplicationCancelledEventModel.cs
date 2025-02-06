using System;

namespace nPreCredit.Code.AffiliateReporting
{

    public class CreditApplicationCancelledEventModel : AffiliateReportingEventModelBase
    {
        public static string EventTypeName = "CreditApplicationCancelled";
        public DateTime CancelledDate { get; set; }
        public bool WasAutomated { get; set; }
    }
}