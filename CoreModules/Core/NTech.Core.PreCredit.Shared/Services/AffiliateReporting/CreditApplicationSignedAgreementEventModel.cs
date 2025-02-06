namespace nPreCredit.Code.AffiliateReporting
{

    public class CreditApplicationSignedAgreementEventModel : AffiliateReportingEventModelBase
    {
        public static string EventTypeName = "CreditApplicationSignedAgreement";
        public int ApplicantNr { get; set; }
        public bool AllApplicantsHaveNowSigned { get; set; }
    }
}