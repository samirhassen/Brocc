namespace nPreCredit.Code.AffiliateReporting.Zmarta
{
    public interface IZmartaWebservice
    {
        HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt);
        HandleEventResult ReportCustomerSignedAgreement(CreditApplicationSignedAgreementEventModel evt);
        HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt);
        HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt);
    }
}