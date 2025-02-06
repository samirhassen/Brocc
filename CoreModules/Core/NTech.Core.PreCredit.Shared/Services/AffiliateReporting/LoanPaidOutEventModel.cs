using System;

namespace nPreCredit.Code.AffiliateReporting
{

    public class LoanPaidOutEventModel : AffiliateReportingEventModelBase
    {
        public static string EventTypeName = "LoanPaidOut";
        public string CreditNr { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}