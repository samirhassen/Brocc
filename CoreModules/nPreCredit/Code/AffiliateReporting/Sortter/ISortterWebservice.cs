namespace nPreCredit.Code.AffiliateReporting.Sortter
{
    public interface ISortterWebservice
    {
        HandleEventResult Approved(CreditDecisionApprovedEventModel evt);
        HandleEventResult Completed(LoanPaidOutEventModel evt);
        HandleEventResult Rejected(CreditApplicationRejectedEventModel evt);
        HandleEventResult Cancelled(CreditApplicationCancelledEventModel evt);
    }
}
