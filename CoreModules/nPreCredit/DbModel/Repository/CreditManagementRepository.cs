using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public class CreditManagementRepository : BaseRepository
    {
        public CreditManagementRepository() : base()
        {

        }

        public CreditManagementRepository(
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName) : base(currentEncryptionKeyName, encryptionKeysByName)
        {
        }

        public class CreditManagementFilter
        {
            public string CreditCheckStatus { get; set; }
            public string CustomerCheckStatus { get; set; }
            public string AgreementStatus { get; set; }
            public string FraudCheckStatus { get; set; }
            public string ProviderName { get; set; }
            public bool? IsActive { get; set; }
            public bool? IsFinalDecisionMade { get; set; }
            public bool? IsPartiallyApproved { get; set; }
            public DateTimeOffset? FirstIncludedDate { get; set; }
        }

        public FilteredCreditManagementResult GetFilteredCreditManagementPage(
            CreditManagementFilter filter,
            int pageSize,
            int pageNr,
            Func<string, string> createUrlFromApplicationNr)
        {
            filter = filter ?? new CreditManagementFilter();

            using (var context = new PreCreditContext())
            {
                var baseResult = context
                    .CreditApplicationHeaders
                    .Where(x =>
                           (!filter.IsActive.HasValue || x.IsActive == filter.IsActive.Value)
                        && (!filter.IsPartiallyApproved.HasValue || x.IsPartiallyApproved == filter.IsPartiallyApproved.Value)
                        && (filter.CreditCheckStatus == null || x.CreditCheckStatus == filter.CreditCheckStatus)
                        && (filter.CustomerCheckStatus == null || x.CustomerCheckStatus == filter.CustomerCheckStatus)
                        && (filter.AgreementStatus == null || x.AgreementStatus == filter.AgreementStatus)
                        && (filter.FraudCheckStatus == null || x.FraudCheckStatus == filter.FraudCheckStatus)
                        && (!filter.IsFinalDecisionMade.HasValue || x.IsFinalDecisionMade == filter.IsFinalDecisionMade.Value)
                        && (!filter.FirstIncludedDate.HasValue || x.ApplicationDate >= filter.FirstIncludedDate.Value)
                        && (filter.ProviderName == null || x.ProviderName == filter.ProviderName)
                    )
                    .OrderBy(x => x.ApplicationNr)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ProviderName,
                        x.ApplicationDate,
                        x.AgreementStatus,
                        x.CreditCheckStatus,
                        x.CustomerCheckStatus,
                        x.FraudCheckStatus,
                        x.IsActive,
                        IsFinalDecisionMade = x.IsFinalDecisionMade,
                        x.ArchivedDate
                    });

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new FilteredCreditManagementResult.Hit
                    {
                        ApplicationNr = x.ApplicationNr,
                        ProviderName = x.ProviderName,
                        ApplicationDate = x.ApplicationDate,
                        AgreementStatus = x.AgreementStatus,
                        CustomerCheckStatus = x.CustomerCheckStatus,
                        FraudCheckStatus = x.FraudCheckStatus,
                        CreditCheckStatus = x.CreditCheckStatus,
                        IsFinalDecisionMade = x.IsFinalDecisionMade,
                        IsActive = x.IsActive,
                        NavigationUrl = createUrlFromApplicationNr(x.ApplicationNr),
                        ArchivedDate = x.ArchivedDate
                    })
                    .ToList();
                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);
                return new FilteredCreditManagementResult
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                };
            }
        }

        public List<ProviderItem> GetAllProviders()
        {
            using (var context = new PreCreditContext())
            {
                return context
                    .CreditApplicationHeaders
                    .Select(x => x.ProviderName)
                    .Distinct()
                    .ToList()
                    .OrderBy(x => x)
                    .Select(x => new ProviderItem { ProviderName = x, DisplayName = x })
                    .ToList();
            }
        }

        public class ProviderItem
        {
            public string ProviderName { get; set; }
            public string DisplayName { get; set; }
        }

        public class FilteredCreditManagementResult
        {
            public class Hit
            {
                public string ApplicationNr { get; set; }
                public string ProviderName { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public string CreditCheckStatus { get; set; }
                public string CustomerCheckStatus { get; set; }
                public string FraudCheckStatus { get; set; }
                public string AgreementStatus { get; set; }
                public bool IsFinalDecisionMade { get; set; }
                public string NavigationUrl { get; set; }
                public bool IsActive { get; set; }
                public DateTimeOffset? ArchivedDate { get; set; }
            }

            public List<Hit> Page { get; set; }

            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
        }
    }
}