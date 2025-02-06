namespace nPreCredit.Code.AffiliateReporting.Eone
{
    public interface IEoneWebservice
    {
        HandleEventResult ReportGrantedApplication(CreditDecisionApprovedEventModel evt);
        HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt);
        HandleEventResult ReportPaymentOnNewCredit(LoanPaidOutEventModel evt);
    }
}