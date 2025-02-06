using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class FuturePaymentFreeMonthBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        public FuturePaymentFreeMonthBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration) : base(currentUser, clock, clientConfiguration)
        {

        }

        public bool HasPendingFuturePaymentFreeMonth(ICreditContextExtended context, string creditNr, DateTime forMonth)
        {
            return context
                .CreditFuturePaymentFreeMonthsQueryable
                .Any(x => x.ForMonth.Year == forMonth.Year
                    && x.ForMonth.Month == forMonth.Month
                    && !x.CancelledByBusinessEventId.HasValue
                    && !x.CommitedByEventBusinessEventId.HasValue
                    && x.CreditNr == creditNr);
        }

        public void AddFuturePaymentFreeMonth(ICreditContextExtended context, string creditNr, DateTime forMonth)
        {
            var evt = AddBusinessEvent(BusinessEventType.AddedFuturePaymentFreeMonth, context);
            var f = new CreditFuturePaymentFreeMonth
            {
                ForMonth = new DateTime(forMonth.Year, forMonth.Month, 1),
                CreditNr = creditNr,
                ChangedById = UserId,
                CreatedByEvent = evt,
                InformationMetaData = InformationMetadata
            };
            FillInInfrastructureFields(f);
            AddComment($"Scheduled a paymentfree month for {FormatMonthCultureAware(f.ForMonth)}", BusinessEventType.AddedFuturePaymentFreeMonth, context, creditNr: creditNr);
            context.AddCreditFuturePaymentFreeMonths(f);
        }

        public void CancelFuturePaymentFreeMonth(ICreditContextExtended context, string creditNr, DateTime forMonth)
        {
            var pending = context
                .CreditFuturePaymentFreeMonthsQueryable
                .Where(x => x.ForMonth.Year == forMonth.Year && x.ForMonth.Month == forMonth.Month && !x.CancelledByBusinessEventId.HasValue && !x.CommitedByEventBusinessEventId.HasValue && x.CreditNr == creditNr)
                .ToList();
            if (pending.Count > 0)
            {
                var evt = AddBusinessEvent(BusinessEventType.AddedFuturePaymentFreeMonth, context);
                foreach (var p in pending)
                    p.CancelledByEvent = evt;
                AddComment($"Removed a scheduled paymentfree month for {FormatMonthCultureAware(forMonth)}", BusinessEventType.RemovedFuturePaymentFreeMonth, context, creditNr: creditNr);
            }
        }
    }
}