using Newtonsoft.Json;
using nPreCredit.DbModel;
using System;

namespace nPreCredit.Code.AffiliateReporting
{
    public abstract class AffiliateCallbackDispatcherBase : IAffiliateCallbackDispatcher
    {
        protected HandleEventResult NotSubscribed()
        {
            return new HandleEventResult
            {
                Message = "Affiliate does not subscribe to this event",
                Status = AffiliateReportingEventResultCode.Ignored
            };
        }

        private HandleEventResult DispatchEvent<T>(AffiliateReportingEvent evt, Func<T, HandleEventResult> handleEvent)
        {
            var d = JsonConvert.DeserializeObject<T>(evt.EventData);
            return handleEvent(d);
        }

        public HandleEventResult Dispatch(AffiliateReportingEvent evt)
        {
            if (evt?.EventType == CreditDecisionApprovedEventModel.EventTypeName)
                return DispatchEvent<CreditDecisionApprovedEventModel>(evt, HandleCreditDecisionApprovedEvent);
            else if (evt?.EventType == CreditApplicationRejectedEventModel.EventTypeName)
                return DispatchEvent<CreditApplicationRejectedEventModel>(evt, HandleCreditApplicationRejectedEvent);
            else if (evt?.EventType == CreditApplicationSignedAgreementEventModel.EventTypeName)
                return DispatchEvent<CreditApplicationSignedAgreementEventModel>(evt, HandleCreditApplicationSignedAgreement);
            else if (evt?.EventType == LoanPaidOutEventModel.EventTypeName)
                return DispatchEvent<LoanPaidOutEventModel>(evt, HandleLoanPaidOutEvent);
            else if (evt?.EventType == CreditApplicationCancelledEventModel.EventTypeName)
                return DispatchEvent<CreditApplicationCancelledEventModel>(evt, HandleCreditApplicationCancelled);
            else
                return NotSubscribed();
        }

        protected virtual HandleEventResult HandleCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel evt)
        {
            return NotSubscribed();
        }

        protected virtual HandleEventResult HandleCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel evt)
        {
            return NotSubscribed();
        }

        protected virtual HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            return NotSubscribed();
        }

        protected virtual HandleEventResult HandleCreditApplicationSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return NotSubscribed();
        }

        protected virtual HandleEventResult HandleCreditApplicationCancelled(CreditApplicationCancelledEventModel evt)
        {
            return NotSubscribed();
        }
    }
}