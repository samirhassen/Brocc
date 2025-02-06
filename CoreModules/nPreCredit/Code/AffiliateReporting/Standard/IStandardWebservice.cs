namespace nPreCredit.Code.AffiliateReporting.Standard
{
    public interface IStandardWebservice
    {
        HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt);
        HandleEventResult ReportCustomerSignedAgreement(CreditApplicationSignedAgreementEventModel evt);
        HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt);
        HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt);
        HandleEventResult ReportCancelledApplication(CreditApplicationCancelledEventModel evt);
    }
}