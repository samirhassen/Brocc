namespace nPreCredit.Code.AffiliateReporting.Salus
{
    public interface ISalusWebservice
    {
        HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt);
        HandleEventResult ReportCustomerSignedAgreement(CreditApplicationSignedAgreementEventModel evt);
        HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt);
        HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt);
    }
}