namespace nPreCredit.Code.AffiliateReporting.Telefinans
{
    public interface ITelefinansWebservice
    {
        HandleEventResult ApprovedV2(CreditDecisionApprovedEventModel evt);
        HandleEventResult Completed(LoanPaidOutEventModel evt);
        HandleEventResult Rejected(CreditApplicationRejectedEventModel evt);
        HandleEventResult Validated(CreditApplicationSignedAgreementEventModel evt);
    }
}
