using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CreditTermsChangeCancelOnlyBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory creditContextFactory;
        protected readonly ICustomerClient customerClient;

        public CreditTermsChangeCancelOnlyBusinessEventManager(INTechCurrentUserMetadata currentUser,
            ICoreClock clock, IClientConfigurationCore clientConfiguration,
            CreditContextFactory creditContextFactory, ICustomerClient customerClient) : base(currentUser, clock, clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.customerClient = customerClient;
        }

        public List<int> GetActiveTermChangeIdsOnCredits(ICreditContextExtended context, ISet<string> creditNrs)
        {
            return context
                .CreditTermsChangeHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr) && !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue)
                .Select(x => x.Id)
                .ToList();
        }

        public bool TryCancelCreditTermsChange(int id, bool isManuallyCancelled, out string failedMessage, string additionalReasonMessage = null)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var isOk = TryCancelCreditTermsChange(context, id, isManuallyCancelled, out failedMessage, additionalReasonMessage: additionalReasonMessage);
                if (isOk)
                {
                    context.SaveChanges();
                }
                return isOk;
            }
        }

        public bool TryCancelCreditTermsChange(ICreditContextExtended context, int id, bool isManuallyCancelled, out string failedMessage, string additionalReasonMessage = null)
        {
            var pre = context
                .CreditTermsChangeHeadersQueryable
                .Select(x => new
                {
                    Header = x,
                    Items = x.Items
                })
                .Where(x => x.Header.Id == id)
                .SingleOrDefault();

            var h = pre?.Header;

            if (h == null)
            {
                failedMessage = "There is no such pending terms change";
                return false;
            }

            var signatureSessionKeyItems = h.Items.Where(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureSessionKey.ToString()).ToList();
            foreach (var i in signatureSessionKeyItems)
            {
                WithCreditNrOnExceptionR(() => customerClient.GetElectronicIdSignatureSession(i.Value, true), h.CreditNr);
            }

            var evt = AddBusinessEvent(BusinessEventType.CancelledCreditTermsChange, context);

            h.CancelledByEvent = evt;

            AddComment($"Change terms - {(isManuallyCancelled ? "manually" : "automatically")} cancelled" + (additionalReasonMessage != null ? " " + additionalReasonMessage : ""), BusinessEventType.CancelledCreditTermsChange, context, creditNr: h.CreditNr);

            failedMessage = null;

            return true;
        }

        protected static T WithCreditNrOnExceptionR<T>(Func<T> f, string creditNr)
        {
            T result = default(T);
            WithCreditNrOnException(() => result = f(), creditNr);
            return result;
        }

        protected static void WithCreditNrOnException(Action a, string creditNr)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                throw new Exception($"CreditNr={creditNr}{Environment.NewLine}{ex.Message}", ex);
            }
        }
    }
}