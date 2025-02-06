namespace nPreCredit.Code.AffiliateReporting.Etua
{
    public class EtuaDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "etua";

        private readonly IEtuaWebservice etuaWebservice;

        public EtuaDispatcher(IEtuaWebservice etuaWebservice)
        {
            this.etuaWebservice = etuaWebservice;
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return etuaWebservice.ReportAcceptedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return etuaWebservice.ReportRejectedApplication(evt);
        }

        protected override HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return etuaWebservice.ReportCustomerSignedAgreement(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return etuaWebservice.ReportLoanPaidToCustomer(evt);
        }
    }
}