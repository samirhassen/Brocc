namespace nPreCredit.Code.AffiliateReporting.Zmarta
{
    public class ZmartaDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "zmarta";

        private readonly IZmartaWebservice zmartaWebservice;

        public ZmartaDispatcher(IZmartaWebservice zmartaWebservice)
        {
            this.zmartaWebservice = zmartaWebservice;
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return this.zmartaWebservice.ReportRejectedApplication(evt);
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return this.zmartaWebservice.ReportAcceptedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return this.zmartaWebservice.ReportCustomerSignedAgreement(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return this.zmartaWebservice.ReportLoanPaidToCustomer(evt);
        }
    }
}