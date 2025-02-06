namespace nPreCredit.Code.AffiliateReporting.Sortter
{
    public class SortterDispatcher : AffiliateCallbackDispatcherBase
    {
        public const string DispatcherName = "sortter";

        private readonly ISortterWebservice sortterWebservice;

        public SortterDispatcher(ISortterWebservice sortterWebservice)
        {
            this.sortterWebservice = sortterWebservice;
        }

        protected override HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return sortterWebservice.Approved(evt);
        }

        protected override HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return sortterWebservice.Rejected(evt);
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return sortterWebservice.Completed(evt);
        }

        protected override HandleEventResult HandleCreditApplicationCancelled(CreditApplicationCancelledEventModel evt)
        {
            return sortterWebservice.Cancelled(evt);
        }
    }
}
