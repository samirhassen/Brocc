namespace nPreCredit.Code.AffiliateReporting.Lendo
{
    public interface ILendoWebservice
    {
        HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt);
        HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt);
        HandleEventResult ReportSendAgreement(CreditApplicationSignedAgreementEventModel evt);
        HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt);
    }
}