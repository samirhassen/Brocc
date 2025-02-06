namespace nPreCredit.Code.AffiliateReporting.Lendo
{
    public class LendoDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "lendo";
        private readonly ILendoWebservice lendoWebservice;

        public LendoDispatcher(ILendoWebservice lendoWebservice)
        {
            this.lendoWebservice = lendoWebservice;
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return lendoWebservice.ReportRejectedApplication(evt);
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return lendoWebservice.ReportAcceptedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return lendoWebservice.ReportSendAgreement(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return lendoWebservice.ReportLoanPaidToCustomer(evt);
        }
    }
}