using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanWorkListService : IMortgageLoanWorkListService
    {
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly ICustomerClient customerClient;
        private readonly PreCreditContextFactoryService preCreditContextFactoryService;
        private readonly Dictionary<string, string> providerDisplayNameCache = new Dictionary<string, string>();

        public MortgageLoanWorkListService(IMortgageLoanWorkflowService mortgageLoanWorkflowService, ICustomerClient customerClient, PreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.customerClient = customerClient;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public WorkListResultPage GetWorkListPage(WorkListFilter filter)
        {
            var hasSeparateWorkList = !string.IsNullOrWhiteSpace(filter.SeparatedWorkListName);

            if (!hasSeparateWorkList && filter.CurrentBlockCode == null)
                throw new ArgumentNullException("filter.CurrentBlockCode");

            var mf = mortgageLoanWorkflowService;

            using (var context = preCreditContextFactoryService.CreateExtendedConcrete())
            {
                Func<IQueryable<CreditApplicationHeader>, IQueryable<CreditApplicationHeader>> filterApplications = apps =>
                {
                    var appsLocal = apps;
                    if (filter.AssignedToHandlerUserId.HasValue)
                    {
                        var userIdString = filter.AssignedToHandlerUserId.Value.ToString();
                        appsLocal = appsLocal.Where(x => x.ComplexApplicationListItems.Any(y =>
                            y.ListName == ApplicationAssignedHandlerService.ListName &&
                            y.Nr == ApplicationAssignedHandlerService.ListRowNr &&
                            y.ItemName == ApplicationAssignedHandlerService.AssignedHandlersItemName &&
                            y.ItemValue == userIdString));
                    }

                    if (filter.OnlyNoHandlerAssignedApplications)
                    {
                        appsLocal = appsLocal.Where(x => !x.ComplexApplicationListItems.Any(y =>
                            y.ListName == ApplicationAssignedHandlerService.ListName &&
                            y.Nr == ApplicationAssignedHandlerService.ListRowNr &&
                            y.ItemName == ApplicationAssignedHandlerService.AssignedHandlersItemName));
                    }
                    return appsLocal;
                };

                var qPre = ApplicationInfoService
                    .GetApplicationInfoQueryable(context, preFilter: filterApplications)
                    .Where(x => !x.IsLead);

                if (hasSeparateWorkList)
                {
                    qPre = qPre.Where(x => x.ListNames.Contains(filter.SeparatedWorkListName));
                }
                else if (mf.Model.SeparatedWorkLists != null && mf.Model.SeparatedWorkLists.Count > 0)
                {
                    var separatedWorkListNames = mf.Model.SeparatedWorkLists.Select(x => x.ListName).ToList();
                    qPre = qPre.Where(x => !x.ListNames.Any(y => separatedWorkListNames.Contains(y)));
                }

                var q = qPre
                    .Where(x => x.IsActive)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationDate,
                        x.ProviderName,
                        x.ListNames
                    });

                List<WorkListResultPage.CodeBlockCount> codeBlockCounts = null;

                if (filter.IncludeCurrentBlockCodeCounts.GetValueOrDefault())
                {
                    var preCodeBlockCounts = q
                        .SelectMany(x => x.ListNames.Select(y => new { x.ApplicationNr, ListName = y }))
                        .GroupBy(x => x.ListName)
                        .Select(x => new { Key = x.Key, Count = x.Count() })
                        .ToDictionary(x => x.Key, x => x.Count);

                    if (hasSeparateWorkList)
                    {
                        codeBlockCounts = new List<WorkListResultPage.CodeBlockCount>
                        {
                            new WorkListResultPage.CodeBlockCount
                            {
                                Code = filter.SeparatedWorkListName,
                                Count = preCodeBlockCounts.ContainsKey(filter.SeparatedWorkListName) ? preCodeBlockCounts[filter.SeparatedWorkListName] : 0
                            }
                        };
                    }
                    else
                    {
                        codeBlockCounts = mortgageLoanWorkflowService
                            .GetStepOrder()
                            .Select(x => mf.GetListName(x, mf.InitialStatusName))
                            .Select(x => new WorkListResultPage.CodeBlockCount
                            {
                                Code = x,
                                Count = preCodeBlockCounts.ContainsKey(x) ? preCodeBlockCounts[x] : 0
                            })
                            .ToList();
                    }
                }

                var pageNr = filter.PageNr ?? 0;
                var pageSize = filter.PageSize ?? 20;

                string currentStepName = null;
                if (!hasSeparateWorkList)
                {
                    currentStepName = mortgageLoanWorkflowService.GetListName(filter.CurrentBlockCode, mortgageLoanWorkflowService.InitialStatusName);
                    q = q.Where(x => x.ListNames.Contains(currentStepName));
                }

                var filteredItems = q;
                var totalCount = filteredItems.Count();

                var pageItems = filteredItems
                    .Select(x => new WorkListResultPage.Application
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        CurrentBlockCode = currentStepName,
                        ProviderName = x.ProviderName,
                        ProviderDisplayName = x.ProviderName,
                        LatestSystemCommentText = context
                            .CreditApplicationComments
                            .Where(y => y.ApplicationNr == x.ApplicationNr && y.EventType != "UserComment" && y.EventType != "HouseholdIncomeEdit")
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.CommentText)
                            .FirstOrDefault(),
                    })
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Select(x => x)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList();

                foreach (var p in pageItems)
                    p.ProviderDisplayName = GetProviderDisplayName(p.ProviderName);

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return new WorkListResultPage
                {
                    Applications = pageItems,
                    CurrentBlockCodeCounts = codeBlockCounts,
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Filter = filter
                };
            }
        }

        public SearchResultPage Search(SearchFilter filter, bool findLeads)
        {
            if (filter.OmniSearchValue == null)
                filter.OmniSearchValue = "";

            var omniCustomerIds = customerClient.FindCustomerIdsOmni(filter.OmniSearchValue);
            if (omniCustomerIds.Count > 0)
                return SearchInternal(filter, omniCustomerIds, null, findLeads);

            return SearchInternal(filter, null, new List<string> { filter.OmniSearchValue }, findLeads);
        }

        public SearchResultPage SearchInternal(SearchFilter filter, List<int> customerIds, List<string> applicationNrs, bool findLeads)
        {
            using (var context = preCreditContextFactoryService.CreateExtendedConcrete())
            {
                if (customerIds != null && customerIds.Any())
                {
                    var cids = customerIds.Select(x => x.ToString()).ToList();
                    //Find applications by customerid
                    applicationNrs = context
                        .CreditApplicationHeaders
                        .Where(x =>
                            (x.Items.Any(y => y.Name == "customerId" && cids.Contains(y.Value)) ||
                            x.ComplexApplicationListItems.Any(y => y.ItemName == "customerIds" && cids.Contains(y.ItemValue)) ||
                            x.CustomerListMemberships.Any(y => customerIds.Contains(y.CustomerId))))
                        .Select(x => x.ApplicationNr)
                        .ToList();
                }

                if (applicationNrs == null || applicationNrs.Count == 0)
                    return new SearchResultPage
                    {
                        Applications = new List<ResultPage.Application>(),
                        CurrentPageNr = 1,
                        Filter = filter,
                        TotalNrOfPages = 0
                    };

                var qPre = ApplicationInfoService
                    .GetApplicationInfoQueryable(context)
                    .Where(x => applicationNrs.Contains(x.ApplicationNr) && x.IsLead == findLeads);

                var totalCount = qPre.Count();

                var pageNr = filter.PageNr ?? 0;
                var pageSize = filter.PageSize ?? 20;

                var pageItems = qPre
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationDate,
                        x.ListNames,
                        x.ProviderName,
                        LatestSystemCommentText = context
                            .CreditApplicationComments
                            .Where(y => y.ApplicationNr == x.ApplicationNr && y.EventType != "UserComment" && y.EventType != "HouseholdIncomeEdit")
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.CommentText)
                            .FirstOrDefault(),
                        x.IsLead,
                        x.IsActive
                    })
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Select(x => x)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList();

                var applications = new List<ResultPage.Application>();
                foreach (var i in pageItems)
                {
                    applications.Add(new ResultPage.Application
                    {
                        ApplicationNr = i.ApplicationNr,
                        ApplicationDate = i.ApplicationDate,
                        LatestSystemCommentText = i.LatestSystemCommentText,
                        ProviderName = i.ProviderName,
                        CurrentBlockCode = mortgageLoanWorkflowService.GetCurrentListName(i.ListNames),
                        ProviderDisplayName = GetProviderDisplayName(i.ProviderName),
                        IsLead = i.IsLead,
                        IsActive = i.IsActive
                    });
                }
                return new SearchResultPage
                {
                    CurrentPageNr = pageNr,
                    Filter = filter,
                    TotalNrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1),
                    Applications = applications
                };
            }
        }

        private string GetProviderDisplayName(string providerName)
        {
            if (providerName == null)
                return providerName;

            if (providerDisplayNameCache.ContainsKey(providerName))
                return providerDisplayNameCache[providerName];

            var n = NEnv.GetAffiliateModel(providerName, allowMissing: true)?.DisplayToEnduserName ?? providerName;
            if (n != null)
                providerDisplayNameCache[providerName] = n;

            return n;
        }

        public abstract class FilterBase
        {
            public int? PageNr { get; set; }
            public int? PageSize { get; set; }
        }

        public class SearchFilter : FilterBase
        {
            public string OmniSearchValue { get; set; }
        }

        public class WorkListFilter : FilterBase
        {
            public string CurrentBlockCode { get; set; }
            public bool? IncludeCurrentBlockCodeCounts { get; set; }
            public string SeparatedWorkListName { get; set; }
            public int? AssignedToHandlerUserId { get; set; }
            public bool OnlyNoHandlerAssignedApplications { get; set; }
        }

        public abstract class ResultPage
        {
            public class Application
            {
                public string ApplicationNr { get; set; }
                public string ProviderName { get; set; }
                public string ProviderDisplayName { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public string LatestSystemCommentText { get; set; }
                public string CurrentBlockCode { get; set; }
                public bool IsLead { get; set; }
                public bool IsActive { get; set; }
            }

            public List<Application> Applications { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
        }

        public class SearchResultPage : ResultPage
        {
            public SearchFilter Filter { get; set; }
        }

        public class WorkListResultPage : ResultPage
        {
            public class CodeBlockCount
            {
                public string Code { get; set; }
                public int Count { get; set; }
            }

            public WorkListFilter Filter { get; set; }
            public List<CodeBlockCount> CurrentBlockCodeCounts { get; set; }
        }
    }

    public interface IMortgageLoanWorkListService
    {
        MortgageLoanWorkListService.WorkListResultPage GetWorkListPage(MortgageLoanWorkListService.WorkListFilter filter);

        MortgageLoanWorkListService.SearchResultPage Search(MortgageLoanWorkListService.SearchFilter filter, bool findLeads);
    }
}