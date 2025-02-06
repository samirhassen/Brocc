namespace nPreCredit.Code.AffiliateReporting.Eone
{
    public class EoneDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "eone";

        private readonly IEoneWebservice eoneWebservice;

        public EoneDispatcher(IEoneWebservice eoneWebservice)
        {
            this.eoneWebservice = eoneWebservice;
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return this.eoneWebservice.ReportGrantedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return this.eoneWebservice.ReportRejectedApplication(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return this.eoneWebservice.ReportPaymentOnNewCredit(evt);
        }
    }
}