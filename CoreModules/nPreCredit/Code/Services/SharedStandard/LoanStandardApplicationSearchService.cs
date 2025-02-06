using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class LoanStandardApplicationSearchService
    {
        private readonly IClock clock;
        private readonly ICustomerClient customerClient;
        private readonly CivicRegNumberParser civicRegNumberParser;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;

        public LoanStandardApplicationSearchService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock, ICustomerClient customerClient, CivicRegNumberParser civicRegNumberParser)
        {
            this.clock = clock;
            this.customerClient = customerClient;
            this.civicRegNumberParser = civicRegNumberParser;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
        }

        private class SearchModel
        {
            public DateTimeOffset? WaitingForAdditionalInformationDate { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public string ApplicationNr { get; set; }
            public bool IsActive { get; set; }
            public string ProviderName { get; set; }
            public string RequestedAmount { get; set; }
            public bool IsPartiallyApproved { get; set; }
            public bool IsFinalDecisionMade { get; set; }
            public string LatestSystemCommentText { get; set; }
            public IEnumerable<string> ListNames { get; set; }
        }

        private PreCreditContextExtended createContext()
        {
            return new PreCreditContextExtended(ntechCurrentUserMetadata, clock);
        }

        private IQueryable<SearchModel> GetSearchModel(PreCreditContextExtended context, bool forceShowUserHiddenItems, string listName, string providerName, ApplicationAssignedHandlerModel assignedHandler, bool showOnlyActive)
        {
            var isForMortgageLoans = NEnv.IsStandardMortgageLoansEnabled;
            var applicationType = (isForMortgageLoans ? CreditApplicationTypeCode.mortgageLoan : CreditApplicationTypeCode.unsecuredLoan).ToString();

            var baseQuery = context
                .CreditApplicationHeaders
                .Where(x => x.ApplicationType == applicationType.ToString());

            if (showOnlyActive)
                baseQuery = baseQuery.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(providerName))
                baseQuery = baseQuery.Where(x => x.ProviderName == providerName);

            if (!string.IsNullOrWhiteSpace(listName))
                baseQuery = baseQuery.Where(x => x.ListMemberships.Any(y => y.ListName == listName));

            if (assignedHandler != null)
            {
                if (!string.IsNullOrWhiteSpace(assignedHandler.AssignedHandlerUserId))
                    baseQuery = baseQuery.Where(x => x.ComplexApplicationListItems.Where(y => y.ListName == "Handlers" && y.ItemName == "AssignedHandlerIds" && y.ItemValue == assignedHandler.AssignedHandlerUserId).Select(r => r.ItemValue).FirstOrDefault() == assignedHandler.AssignedHandlerUserId);

                if (assignedHandler.ExcludeAssignedApplications)
                    baseQuery = baseQuery.Where(x => x.ComplexApplicationListItems.Where(y => y.ListName == "Handlers" && y.ItemName == "AssignedHandlerIds").FirstOrDefault() == null);

                if (assignedHandler.ExcludeUnassignedApplications)
                    baseQuery = baseQuery.Where(x => x.ComplexApplicationListItems.Where(y => y.ListName == "Handlers" && y.ItemName == "AssignedHandlerIds").FirstOrDefault() != null);
            }

            if (!forceShowUserHiddenItems)
                baseQuery = baseQuery
                    .Where(x => (!x.HideFromManualListsUntilDate.HasValue || x.HideFromManualListsUntilDate < clock.Now));

            return baseQuery
                .Select(x => new
                {
                    x.ApplicationNr,
                    x.IsActive,
                    x.ApplicationDate,
                    x.ProviderName,
                    x.WaitingForAdditionalInformationDate,
                    x.IsPartiallyApproved,
                    x.IsFinalDecisionMade,
                    ListNames = x.ListMemberships.Select(y => y.ListName),
                    LatestSystemCommentText = x.Comments
                        .Where(e => e.EventType != "UserComment")
                        .OrderByDescending(c => c.CommentDate)
                        .FirstOrDefault()
                        .CommentText,
                    RequestedAmount = isForMortgageLoans ? "0" : (x.ComplexApplicationListItems
                        .FirstOrDefault(item => item.ListName == "Application" && item.ItemName == "requestedLoanAmount")
                        .ItemValue)
                })
                .Select(x => new SearchModel
                {
                    ApplicationDate = x.ApplicationDate,
                    ApplicationNr = x.ApplicationNr,
                    IsActive = x.IsActive,
                    IsPartiallyApproved = x.IsPartiallyApproved,
                    IsFinalDecisionMade = x.IsFinalDecisionMade,
                    WaitingForAdditionalInformationDate = x.WaitingForAdditionalInformationDate,
                    LatestSystemCommentText = x.LatestSystemCommentText,
                    ProviderName = x.ProviderName,
                    ListNames = x.ListNames,
                    RequestedAmount = x.RequestedAmount
                });
        }

        public int GetAssignabeCount(bool forceShowUserHiddenItems)
        {
            using (var c = createContext())
            {
                return GetSearchModel(c, forceShowUserHiddenItems, null, null, new ApplicationAssignedHandlerModel { ExcludeAssignedApplications = true }, true).Count();
            }
        }

        public DataPageResult GetDataPage(string listName, string providerName, ApplicationAssignedHandlerModel assignedHandler, bool forceShowUserHiddenItems, int pageNr, int pageSize, bool includeListCounts)
        {
            using (var c = createContext())
            {
                Func<string, IQueryable<SearchModel>> getBaseItems = listNameFilter =>
                    GetSearchModel(c, forceShowUserHiddenItems, listNameFilter, providerName, assignedHandler, true);

                var b = getBaseItems(listName);

                var totalCount = b.Count();
                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                var dataPage =
                     b
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(ToHitModel)
                    .ToList();

                Dictionary<string, int> listCounts = null;
                if (includeListCounts)
                {
                    listCounts = getBaseItems(null)
                        .SelectMany(x => x.ListNames.Select(y => new { x.ApplicationNr, ListName = y }))
                        .GroupBy(x => x.ListName)
                        .ToDictionary(x => x.Key, x => x.Count());
                }

                return new DataPageResult
                {
                    PageApplications = dataPage,
                    ListCountsByName = listCounts,
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages
                };
            }
        }

        public List<LoanStandardApplicationSearchHit> Search(string omniSearchValue, bool forceShowUserHiddenItems)
        {
            Func<List<string>, PreCreditContextExtended, List<LoanStandardApplicationSearchHit>> fromApplicationNrs = (applicationNrs, context) =>
                GetSearchModel(context, forceShowUserHiddenItems, null, null, null, false)
                    .Where(x => applicationNrs.Contains(x.ApplicationNr))
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .ToList()
                    .Select(ToHitModel)
                    .ToList();

            Func<List<int>, List<LoanStandardApplicationSearchHit>> fromCustomerIds = (customerIds) =>
            {
                using (var c = createContext())
                {
                    var listNamesToConsider = new List<string> { "Applicant" };
                    List<string> applicationNrs = c
                        .CreditApplicationCustomerListMembers
                        .Where(x => listNamesToConsider.Contains(x.ListName) && customerIds.Contains(x.CustomerId))
                        .Select(x => x.ApplicationNr)
                        .ToList();

                    return fromApplicationNrs(applicationNrs, c);
                }
            };

            //Try exact match for application nr first
            using (var c = createContext())
            {
                var applicationNrSearchHits = fromApplicationNrs(new List<string> { omniSearchValue }, c);
                if (applicationNrSearchHits.Count > 0)
                    return applicationNrSearchHits;
            }

            //Otherwise customer
            var customerSearchCustomerIds = customerClient.FindCustomerIdsOmni(omniSearchValue);
            return fromCustomerIds(customerSearchCustomerIds);
        }

        private LoanStandardApplicationSearchHit ToHitModel(SearchModel x)
        {
            return new LoanStandardApplicationSearchHit
            {
                ApplicationNr = x.ApplicationNr,
                ApplicationDate = x.ApplicationDate,
                LatestSystemCommentText = x.LatestSystemCommentText ?? "No comments",
                LatestSystemCommentDate = null,
                IsFinalDecisionMade = x.IsFinalDecisionMade,
                IsPartiallyApproved = x.IsPartiallyApproved,
                ProviderName = x.ProviderName,
                IsActive = x.IsActive,
                RequestedAmount = x.RequestedAmount != null ? Convert.ToInt32(x.RequestedAmount) : (int?)null
            };
        }

        public class DataPageResult
        {
            public List<LoanStandardApplicationSearchHit> PageApplications { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public Dictionary<string, int> ListCountsByName { get; set; }
        }
    }

    public class LoanStandardApplicationSearchHit
    {
        public string ApplicationNr { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsPartiallyApproved { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public string LatestSystemCommentText { get; set; }
        public DateTimeOffset? LatestSystemCommentDate { get; set; }
        public string ProviderName { get; set; }
        public int? RequestedAmount { get; set; }
    }

    public class ApplicationAssignedHandlerModel
    {
        public string AssignedHandlerUserId { get; set; }
        public bool ExcludeAssignedApplications { get; set; }
        public bool ExcludeUnassignedApplications { get; set; }
    }

    public class AssignedApplicationsCountModel
    {
        public int AssignedApplicationsCount { get; set; }
        public int UnassignedApplicationsCount { get; set; }
    }

}