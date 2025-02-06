using nCredit.DomainModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace nCredit.DbModel.BusinessEvents
{
    public class PaymentPlacementBatchDataSource
    {
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentOrderService paymentOrderService;
        private HashSet<string> cachedCreditNrs = new HashSet<string>();
        private Dictionary<string, CreditDomainModel> cachedCredits = new Dictionary<string, CreditDomainModel>();
        private Dictionary<string, Dictionary<int, CreditNotificationDomainModel>> cachedNotifications = new Dictionary<string, Dictionary<int, CreditNotificationDomainModel>>();
        private Dictionary<string, List<CreditNotificationHeader>> cachedOpenNotificationHeadersByCreditNr = new Dictionary<string, List<CreditNotificationHeader>>();
        private Dictionary<string, (bool HasSettlementOffer, CreditSettlementOfferHeader Offer)> cachedActiveSettlementOffers = new Dictionary<string, (bool HasSettlementOffer, CreditSettlementOfferHeader Offer)>();
        private Dictionary<string, CreditHeader> cachedCreditHeaders = new Dictionary<string, CreditHeader>();

        public PaymentPlacementBatchDataSource(ICreditEnvSettings envSettings, PaymentOrderService paymentOrderService)
        {
            this.envSettings = envSettings;
            this.paymentOrderService = paymentOrderService;
        }

        public CreditDomainModel GetCreditDomainModel(string creditNr, ICreditContextExtended context)
        {
            if (cachedCredits?.ContainsKey(creditNr) == true)
                return cachedCredits[creditNr];

            return CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);
        }

        public Dictionary<int, CreditNotificationDomainModel> GetOpenNotificationDomainModels(string creditNr, ICreditContextExtended context)
        {
            if (cachedNotifications?.ContainsKey(creditNr) == true)
                return cachedNotifications[creditNr];

            return CreditNotificationDomainModel.CreateForCredit(creditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: true);
        }

        public decimal GetCurrentBalanceOnOpenNotification(string creditNr, int notificationId, ICreditContextExtended context, IEnumerable<AccountTransaction> additionalTransactions = null)
        {
            var openNotifications = GetOpenNotificationDomainModels(creditNr, context);
            CreditNotificationTransactionsModel notification = openNotifications[notificationId];
            if (additionalTransactions != null)
                notification = CreditNotificationTransactionsModel.AppendTransactions(notification, additionalTransactions);
            return notification.GetRemainingBalance(context.CoreClock.Today);
        }

        public CreditNotificationHeader GetCreditNotificationHeader(string creditNr, int notificationId, ICreditContextExtended context)
        {
            var creditNotifications = cachedOpenNotificationHeadersByCreditNr.Opt(creditNr);
            if (creditNotifications != null)
                return creditNotifications.Single(x => x.Id == notificationId);
            else
                return context.CreditNotificationHeadersQueryable.Single(x => x.Id == notificationId);
        }

        public CreditHeader GetCreditHeader(string creditNr, ICreditContextExtended context)
        {
            if (cachedCreditHeaders.ContainsKey(creditNr))
                return cachedCreditHeaders[creditNr];

            return context.CreditHeadersQueryable.Single(x => x.CreditNr == creditNr);
        }

        public void EnsurePreloaded(ISet<string> creditNrs, ICreditContextExtended context)
        {
            var allCreditNrsToLoad = creditNrs.Except(cachedCreditNrs).ToHashSetShared();
            if (allCreditNrsToLoad.Count == 0)
                return;

            foreach (var groupToLoad in allCreditNrsToLoad.ToArray().SplitIntoGroupsOfN(300))
            {
                var creditNrsToLoad = groupToLoad.ToHashSetShared();

                cachedCreditNrs.UnionWith(creditNrsToLoad);
                cachedCredits.AddOrReplaceFrom(CreditDomainModel.PreFetchForCredits(context, creditNrsToLoad.ToArray(), envSettings));
                cachedNotifications.AddOrReplaceFrom(CreditNotificationDomainModel.CreateForSeveralCredits(creditNrsToLoad, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: true));

                //Notification headers
                cachedOpenNotificationHeadersByCreditNr.AddOrReplaceFrom(context.CreditNotificationHeadersQueryable.Where(x => creditNrsToLoad.Contains(x.CreditNr) && x.ClosedTransactionDate == null)
                    .ToList().GroupBy(x => x.CreditNr).ToDictionary(x => x.Key, x => x.ToList()));

                //Settlement offers
                var settlementOffers = context
                    .CreditSettlementOfferHeadersQueryable
                    .Where(x => creditNrsToLoad.Contains(x.CreditNr) && !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue)
                    .ToList()
                    .GroupBy(x => x.CreditNr)
                    .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Id).First());
                cachedActiveSettlementOffers.AddOrReplaceFrom(creditNrsToLoad
                    .ToDictionary(x => x, x => (HasSettlementOffer: settlementOffers.ContainsKey(x), Offer: settlementOffers.Opt(x))));

                cachedCreditHeaders.AddOrReplaceFrom(
                    context.CreditHeadersQueryable.Where(x => creditNrsToLoad.Contains(x.CreditNr)).ToDictionary(x => x.CreditNr, x => x));
            }
        }

        public bool HasActiveSettlementOffer(string creditNr) => cachedActiveSettlementOffers.OptS(creditNr)?.HasSettlementOffer == true;

        public CreditSettlementOfferHeader GetActiveSettlementOffer(string creditNr) => cachedActiveSettlementOffers.OptS(creditNr)?.Offer;
    }
};