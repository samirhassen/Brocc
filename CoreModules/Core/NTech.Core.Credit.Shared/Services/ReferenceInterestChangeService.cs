using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class ReferenceInterestChangeService : IReferenceInterestChangeService
    {
        private readonly Func<string, string> getUserDisplayNameByUserId;
        private readonly IKeyValueStoreService keyValueStoreService;

        public ReferenceInterestChangeService(Func<string, string> getUserDisplayNameByUserId,
            IKeyValueStoreService keyValueStoreService, ICoreClock clock, LegalInterestCeilingService legalInterestCeilingService,
            ICreditEnvSettings envSettings, IClientConfigurationCore clientCfg,
            CreditContextFactory creditContextFactory)
        {
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.keyValueStoreService = keyValueStoreService;
            this.clock = clock;
            this.creditContextFactory = creditContextFactory;
            createReferenceInterestRateChangeBusinessEventManager = user => new ReferenceInterestRateChangeBusinessEventManager(user,
                legalInterestCeilingService, envSettings, clock, clientCfg);
        }

        private const string ReferenceInterestChangeStorageKey = "PendingReferenceInterestChangeV1";
        private readonly ICoreClock clock;
        private readonly CreditContextFactory creditContextFactory;
        private readonly Func<INTechCurrentUserMetadata, ReferenceInterestRateChangeBusinessEventManager> createReferenceInterestRateChangeBusinessEventManager;

        public PendingReferenceInterestChangeModel BeginChangeReferenceInterest(
            decimal newInterestRatePercent,
            INTechCurrentUserMetadata currentUser)
        {
            var m = new PendingReferenceInterestChangeModel
            {
                InitiatedByUserId = currentUser.UserId,
                NewInterestRatePercent = newInterestRatePercent,
                InitiatedDate = clock.Now
            };
            this.keyValueStoreService.SetValue(ReferenceInterestChangeStorageKey, ReferenceInterestChangeStorageKey, JsonConvert.SerializeObject(m));
            return m;
        }

        public void CancelChangeReferenceInterest(Action<bool> observeWasAnyCancelled = null)
        {
            this.keyValueStoreService.RemoveValue(
                ReferenceInterestChangeStorageKey, ReferenceInterestChangeStorageKey,
                observeWasRemoved: observeWasAnyCancelled);
        }

        public PendingReferenceInterestChangeModel GetPendingReferenceInterestChange()
        {
            var v = this.keyValueStoreService.GetValue(ReferenceInterestChangeStorageKey, ReferenceInterestChangeStorageKey);
            if (v == null)
                return null;
            return JsonConvert.DeserializeObject<PendingReferenceInterestChangeModel>(v);
        }

        public bool TryChangeReferenceInterest(
            PendingReferenceInterestChangeModel pendingChange,
            INTechCurrentUserMetadata currentUser,
            out string failMessage,
            Action<int> observeNrOfCreditsUpdated = null)
        {
            var mgr = createReferenceInterestRateChangeBusinessEventManager(currentUser);
            using (var context = creditContextFactory.CreateContext())
            {
                BusinessEvent evt;
                if (mgr.TryChangeReferenceInterest(
                    context,
                    pendingChange.NewInterestRatePercent,
                    out evt,
                    out failMessage,
                    initiatedByAndDate: pendingChange == null
                        ? null
                        : Tuple.Create(pendingChange.InitiatedByUserId, pendingChange.InitiatedDate.DateTime)))
                {
                    var wasRemoved = KeyValueStoreService.RemoveValueComposable(context, ReferenceInterestChangeStorageKey, ReferenceInterestChangeStorageKey);
                    if (wasRemoved)
                    {
                        context.SaveChanges();
                        observeNrOfCreditsUpdated?.Invoke(evt.DatedCreditValues?.Count ?? 0);
                        return true;
                    }
                    else
                    {
                        failMessage = "The pending change had already been removed by something else";
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public ReferenceInterestChangePage GetReferenceInterestRateChangesPage(int pageSize, int pageNr = 0)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var baseResult = context
                    .BusinessEventsQueryable
                    .Where(x => x.EventType == BusinessEventType.ReferenceInterestRateChange.ToString());

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.ChangedDate,
                        x.TransactionDate,
                        ChangedToValue = x.SharedDatedValues.Select(y => y.Value).FirstOrDefault(),
                        ChangedCreditCount = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString()).Count(),
                        UserId = x.ChangedById,
                        ChangeHeader = x.CreatedReferenceInterestChangeHeaders.FirstOrDefault()
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var initiatedByUserId = x.ChangeHeader != null ? x.ChangeHeader.InitiatedByUserId : x.UserId;
                        return new ReferenceInterestChangePage.PageModel
                        {
                            ApprovedDate = x.ChangedDate.DateTime,
                            TransactionDate = x.TransactionDate,
                            UserId = x.UserId,
                            UserDisplayName = getUserDisplayNameByUserId(x.UserId.ToString()),
                            ChangedToValue = x.ChangedToValue,
                            ChangedCreditCount = x.ChangedCreditCount,
                            InitiatedByUserId = initiatedByUserId,
                            InitiatedByDisplayName = getUserDisplayNameByUserId(initiatedByUserId.ToString()),
                            InitiatedDate = x.ChangeHeader != null ? x.ChangeHeader.InitiatedDate : x.TransactionDate
                        };
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return new ReferenceInterestChangePage
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                };
            }
        }
    }

    public interface IReferenceInterestChangeService
    {
        bool TryChangeReferenceInterest(PendingReferenceInterestChangeModel pendingChange,
            INTechCurrentUserMetadata currentUser,
            out string failMessage,
            Action<int> observeNrOfCreditsUpdated = null);
        ReferenceInterestChangePage GetReferenceInterestRateChangesPage(int pageSize, int pageNr = 0);
        PendingReferenceInterestChangeModel GetPendingReferenceInterestChange();
        PendingReferenceInterestChangeModel BeginChangeReferenceInterest(
            decimal newInterestRatePercent,
            INTechCurrentUserMetadata currentUser);
        void CancelChangeReferenceInterest(Action<bool> observeWasAnyCancelled = null);
    }

    public class ReferenceInterestChangePage
    {
        public int CurrentPageNr { get; set; }
        public int TotalNrOfPages { get; set; }
        public List<PageModel> Page { get; set; }

        public class PageModel
        {
            public DateTime ApprovedDate { get; set; }
            public DateTime TransactionDate { get; set; }
            public int InitiatedByUserId { get; set; }
            public string InitiatedByDisplayName { get; set; }
            public DateTime InitiatedDate { get; set; }
            public int UserId { get; set; }
            public string UserDisplayName { get; set; }
            public decimal ChangedToValue { get; set; }
            public int ChangedCreditCount { get; set; }
        }
    }

    public class PendingReferenceInterestChangeModel
    {
        public decimal NewInterestRatePercent { get; set; }
        public int InitiatedByUserId { get; set; }
        public DateTimeOffset InitiatedDate { get; set; }
    }
}