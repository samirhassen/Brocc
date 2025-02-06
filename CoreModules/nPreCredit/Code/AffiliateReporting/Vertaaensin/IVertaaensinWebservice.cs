namespace nPreCredit.Code.AffiliateReporting.Vertaaensin
{
    public interface IVertaaensinWebservice
    {
        HandleEventResult ReportApproved(CreditDecisionApprovedEventModel evt);
        HandleEventResult ReportRejected(CreditApplicationRejectedEventModel evt);
        HandleEventResult ReportLoanPaidOut(LoanPaidOutEventModel evt);
    }
}