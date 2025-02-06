namespace nPreCredit.Code.AffiliateReporting.Standard
{
    public class StandardDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "standard";

        private readonly IStandardWebservice standardWebservice;

        public StandardDispatcher(IStandardWebservice standardWebservice)
        {
            this.standardWebservice = standardWebservice;
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return this.standardWebservice.ReportRejectedApplication(evt);
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return this.standardWebservice.ReportAcceptedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return this.standardWebservice.ReportCustomerSignedAgreement(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return this.standardWebservice.ReportLoanPaidToCustomer(evt);
        }

        protected override HandleEventResult HandleCreditApplicationCancelled(CreditApplicationCancelledEventModel evt)
        {
            return this.standardWebservice.ReportCancelledApplication(evt);
        }
    }
}