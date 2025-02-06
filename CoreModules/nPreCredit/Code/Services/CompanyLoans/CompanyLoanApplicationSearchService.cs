using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CompanyLoanApplicationSearchService : ICompanyLoanApplicationSearchService
    {
        private readonly IClock clock;
        private readonly ICustomerClient customerClient;
        private readonly CivicRegNumberParser civicRegNumberParser;
        private readonly OrganisationNumberParser organisationNumberParser;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;

        public CompanyLoanApplicationSearchService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock, ICustomerClient customerClient, CivicRegNumberParser civicRegNumberParser, OrganisationNumberParser organisationNumberParser)
        {
            this.clock = clock;
            this.customerClient = customerClient;
            this.civicRegNumberParser = civicRegNumberParser;
            this.organisationNumberParser = organisationNumberParser;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
        }

        private class SearchModel
        {
            public DateTimeOffset? WaitingForAdditionalInformationDate { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public string ApplicationNr { get; set; }
            public bool IsActive { get; set; }
            public string LatestSystemCommentText { get; set; }
            public DateTimeOffset? LatestSystemCommentDate { get; set; }
            public string ProviderName { get; set; }
            public string Amount { get; set; }
            public bool IsPartiallyApproved { get; set; }
            public bool IsFinalDecisionMade { get; set; }
            public IEnumerable<string> ListNames { get; set; }
        }

        private PreCreditContextExtended createContext()
        {
            return new PreCreditContextExtended(ntechCurrentUserMetadata, clock);
        }

        private IQueryable<SearchModel> GetSearchModel(PreCreditContextExtended context, bool forceShowUserHiddenItems, string listName, string providerName, bool showOnlyActive)
        {
            var baseQuery = context
                .CreditApplicationHeaders
                .Where(x => x.ApplicationType == CreditApplicationTypeCode.companyLoan.ToString());

            if (showOnlyActive)
                baseQuery = baseQuery.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(providerName))
                baseQuery = baseQuery.Where(x => x.ProviderName == providerName);

            if (!string.IsNullOrWhiteSpace(listName))
                baseQuery = baseQuery.Where(x => x.ListMemberships.Any(y => y.ListName == listName));

            if (!forceShowUserHiddenItems)
                baseQuery = baseQuery
                    .Where(x => (!x.HideFromManualListsUntilDate.HasValue || x.HideFromManualListsUntilDate < clock.Now));

            return baseQuery
                .Select(x => new
                {
                    ApplicationNr = x.ApplicationNr,
                    IsActive = x.IsActive,
                    ApplicationDate = x.ApplicationDate,
                    ProviderName = x.ProviderName,
                    LatestSystemComment = x.Comments.Where(y => y.EventType != "UserComment").OrderByDescending(y => y.Id).FirstOrDefault(),
                    Amount = x.Items.Where(y => y.GroupName == "application" && y.Name == "amount").Select(y => y.Value).FirstOrDefault(),
                    WaitingForAdditionalInformationDate = x.WaitingForAdditionalInformationDate,
                    x.IsPartiallyApproved,
                    x.IsFinalDecisionMade,
                    ListNames = x.ListMemberships.Select(y => y.ListName)
                })
                .Select(x => new SearchModel
                {
                    Amount = x.Amount,
                    ApplicationDate = x.ApplicationDate,
                    ApplicationNr = x.ApplicationNr,
                    IsActive = x.IsActive,
                    IsPartiallyApproved = x.IsPartiallyApproved,
                    IsFinalDecisionMade = x.IsFinalDecisionMade,
                    WaitingForAdditionalInformationDate = x.WaitingForAdditionalInformationDate,
                    LatestSystemCommentDate = x.LatestSystemComment.CommentDate,
                    LatestSystemCommentText = x.LatestSystemComment.CommentText,
                    ProviderName = x.ProviderName,
                    ListNames = x.ListNames
                });
        }

        public DataPageResult GetDataPage(string listName, string providerName, bool forceShowUserHiddenItems, int pageNr, int pageSize, bool includeListCounts)
        {
            using (var c = createContext())
            {
                Func<string, IQueryable<SearchModel>> getBaseItems = listNameFilter =>
                    GetSearchModel(c, forceShowUserHiddenItems, listNameFilter, providerName, true);

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

        public List<CompanyLoanApplicationSearchHit> Search(string omniSearchValue, bool forceShowUserHiddenItems)
        {
            Func<List<string>, PreCreditContextExtended, List<CompanyLoanApplicationSearchHit>> fromApplicationNrs = (applicationNrs, context) =>
                GetSearchModel(context, forceShowUserHiddenItems, null, null, false)
                    .Where(x => applicationNrs.Contains(x.ApplicationNr))
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .ToList()
                    .Select(ToHitModel)
                    .ToList();

            Func<List<string>, List<CompanyLoanApplicationSearchHit>> fromCustomerIds = (customerIds) =>
            {
                using (var c = createContext())
                {
                    List<string> applicationNrs = c.CreditApplicationItems
                        .Where(x => (x.GroupName == "application" && x.Name == "companyCustomerId" && customerIds.Contains(x.Value)) || (x.GroupName == "application" && x.Name == "applicantCustomerId" && customerIds.Contains(x.Value)))
                        .Select(x => x.ApplicationNr)
                        .ToList();

                    return fromApplicationNrs(applicationNrs, c);
                }
            };


            using (var c = createContext())
            {
                var applicationNrHits = fromApplicationNrs(new List<string> { omniSearchValue }, c);
                if (applicationNrHits.Count > 0)
                    return applicationNrHits;
            }

            return fromCustomerIds(customerClient.FindCustomerIdsOmni(omniSearchValue).Select(x => x.ToString()).ToList());
        }

        private CompanyLoanApplicationSearchHit ToHitModel(SearchModel x)
        {
            return new CompanyLoanApplicationSearchHit
            {
                ApplicationNr = x.ApplicationNr,
                ApplicationDate = x.ApplicationDate,
                LatestSystemCommentText = x.LatestSystemCommentText,
                LatestSystemCommentDate = x.LatestSystemCommentDate,
                IsFinalDecisionMade = x.IsFinalDecisionMade,
                IsPartiallyApproved = x.IsPartiallyApproved,
                ProviderName = x.ProviderName,
                IsActive = x.IsActive,
                Amount = StringItem.ParseDecimal(x.Amount)
            };
        }

        public class DataPageResult
        {
            public List<CompanyLoanApplicationSearchHit> PageApplications { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public Dictionary<string, int> ListCountsByName { get; set; }
        }
    }

    public interface ICompanyLoanApplicationSearchService
    {
        List<CompanyLoanApplicationSearchHit> Search(string omniSearchValue, bool forceShowUserHiddenItems);
        CompanyLoanApplicationSearchService.DataPageResult GetDataPage(string listName, string providerName, bool forceShowUserHiddenItems, int pageNr, int pageSize, bool includeListCounts);
    }

    public class CompanyLoanApplicationSearchHit
    {
        public string ApplicationNr { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsPartiallyApproved { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public string LatestSystemCommentText { get; set; }
        public DateTimeOffset? LatestSystemCommentDate { get; set; }
        public decimal? Amount { get; set; }
        public string ProviderName { get; set; }
    }
}