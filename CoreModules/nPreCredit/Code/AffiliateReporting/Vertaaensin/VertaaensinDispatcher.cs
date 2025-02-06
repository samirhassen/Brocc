namespace nPreCredit.Code.AffiliateReporting.Vertaaensin
{
    public class VertaaensinDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "vertaaensin";

        private readonly IVertaaensinWebservice webservice;

        public VertaaensinDispatcher(IVertaaensinWebservice webservice)
        {
            this.webservice = webservice;
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return webservice.ReportApproved(evt);
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return webservice.ReportRejected(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return webservice.ReportLoanPaidOut(evt);
        }
    }
}