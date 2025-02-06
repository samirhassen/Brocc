using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public class CreditDecisionRepository : BaseRepository
    {
        public CreditDecisionRepository() : base()
        {

        }

        public CreditDecisionRepository(
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName) : base(currentEncryptionKeyName, encryptionKeysByName)
        {
        }

        public FilteredCreditDecisionResult GetFilteredCreditDecisionPage(int pageSize, int pageNr, Func<string, string> applicationNrToNavigationUrl, Func<string, string> getUserDisplayNameByUserId)
        {
            using (var context = new PreCreditContext())
            {
                var baseResult = context
                    .CreditApplicationHeaders
                    .Where(x => x.IsActive && x.IsPartiallyApproved && !x.IsFinalDecisionMade)
                    .OrderBy(x => x.ApplicationNr)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ProviderName,
                        x.ApplicationDate,
                        PartiallyApprovedById = x.PartiallyApprovedById.Value
                    });
                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new FilteredCreditDecisionResult.Hit
                    {
                        ApplicationNr = x.ApplicationNr,
                        ProviderName = x.ProviderName,
                        ApplicationDate = x.ApplicationDate,
                        PartiallyApprovedById = x.PartiallyApprovedById,
                        PartiallyApprovedByDisplayName = getUserDisplayNameByUserId(x.PartiallyApprovedById.ToString()),
                        NavigationUrl = applicationNrToNavigationUrl(x.ApplicationNr)
                    })
                    .ToList();
                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);
                return new FilteredCreditDecisionResult
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                };
            }
        }

        public class FilteredCreditDecisionResult
        {
            public class Hit
            {
                public string ApplicationNr { get; set; }
                public string ProviderName { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public int PartiallyApprovedById { get; set; }
                public string PartiallyApprovedByDisplayName { get; set; }
                public string NavigationUrl { get; set; }
            }
            public int CurrentPageNr { get; set; }
            public List<Hit> Page { get; set; }
            public int TotalNrOfPages { get; set; }
        }
    }
}