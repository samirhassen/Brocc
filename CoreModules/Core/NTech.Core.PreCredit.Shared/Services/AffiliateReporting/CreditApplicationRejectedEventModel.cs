using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.AffiliateReporting
{
    public class CreditApplicationRejectedEventModel : AffiliateReportingEventModelBase
    {
        public static string EventTypeName = "CreditApplicationRejected";
        public List<string> RejectionReasons { get; set; }

        public bool IsRejectedDueToPaymentRemark()
        {
            return (RejectionReasons?.Any(x => (x ?? "").ToLowerInvariant().Contains("paymentremark")) ?? false);
        }
    }
}