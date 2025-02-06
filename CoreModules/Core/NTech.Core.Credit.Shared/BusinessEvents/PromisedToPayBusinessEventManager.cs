using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class PromisedToPayBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        public PromisedToPayBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration) : base(currentUser, clock, clientConfiguration)
        {
        }

        private DatedCreditDate GetCurrentPromisedToPayDate(ICreditContextExtended context, string creditNr)
        {
            var latest = context.DatedCreditDatesQueryable.Where(x => x.CreditNr == creditNr && x.Name == DatedCreditDateCode.PromisedToPayDate.ToString()).OrderByDescending(x => x.Timestamp).FirstOrDefault();
            if (latest != null && !latest.RemovedByBusinessEventId.HasValue)
            {
                return latest;
            }
            else
                return null;
        }

        public bool TryAdd(string creditNr, DateTime promisedToPayDate, bool avoidReaddingSameValue, ICreditContextExtended context)
        {
            if (avoidReaddingSameValue)
            {
                var latest = GetCurrentPromisedToPayDate(context, creditNr);
                if (latest != null && latest.Value == promisedToPayDate)
                    return false;
            }

            var evt = AddBusinessEvent(BusinessEventType.AddedPromisedToPayDate, context);

            AddDatedCreditDate(DatedCreditDateCode.PromisedToPayDate, promisedToPayDate, evt, context, creditNr: creditNr);

            AddComment($"Added promised to pay date {promisedToPayDate.ToString("d", CommentFormattingCulture)}", BusinessEventType.AddedPromisedToPayDate, context, creditNr: creditNr);

            return true;
        }

        public bool TryRemove(string creditNr, ICreditContextExtended context)
        {
            var latest = GetCurrentPromisedToPayDate(context, creditNr);
            if (latest == null)
                return false;

            var evt = AddBusinessEvent(BusinessEventType.RemovedPromisedToPayDate, context);

            latest.RemovedByBusinessEvent = evt;

            AddComment("Removed promised to pay date", BusinessEventType.RemovedPromisedToPayDate, context, creditNr: creditNr);

            return true;
        }
    }
}