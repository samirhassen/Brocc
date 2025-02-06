namespace nPreCredit.Code.AffiliateReporting.Etua
{
    public interface IEtuaWebservice
    {
        HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt);

        HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt);

        HandleEventResult ReportCustomerSignedAgreement(CreditApplicationSignedAgreementEventModel evt);

        HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt);
    }
}
