namespace nPreCredit.Code.AffiliateReporting.Telefinans
{
    public class TelefinansDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "telefinans";

        private readonly ITelefinansWebservice telefinansWebservice;

        public TelefinansDispatcher(ITelefinansWebservice telefinansWebservice)
        {
            this.telefinansWebservice = telefinansWebservice;
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return telefinansWebservice.ApprovedV2(evt);
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return telefinansWebservice.Rejected(evt);
        }

        protected override HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            if (evt.AllApplicantsHaveNowSigned)
                return telefinansWebservice.Validated(evt);
            else
                return NotSubscribed();
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return telefinansWebservice.Completed(evt);
        }
    }
}
