namespace nPreCredit.Code.AffiliateReporting.Salus
{
    public class SalusDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "salus";

        private readonly ISalusWebservice salusWebservice;

        public SalusDispatcher(ISalusWebservice salusWebservice)
        {
            this.salusWebservice = salusWebservice;
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return this.salusWebservice.ReportRejectedApplication(evt);
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return this.salusWebservice.ReportAcceptedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return this.salusWebservice.ReportCustomerSignedAgreement(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return this.salusWebservice.ReportLoanPaidToCustomer(evt);
        }
    }
}