using Dapper;
using nCustomer.DbModel;
using NTech;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class CompanyLoanNameSearchService : ICompanyLoanNameSearchService
    {
        private readonly IClock clock;

        public CompanyLoanNameSearchService(IClock clock)
        {
            this.clock = clock;
        }

        private class SearchHit
        {
            public int CustomerId { get; set; }
            public int IsExactMatch { get; set; }
        }

        public ISet<int> FindCustomerByCompanyName(string companyName, Action<string> logDebugData = null)
        {
            var normalizedName = CompanyLoanSearchTerms.NormalizeCompanyName(companyName);
            if (normalizedName == null)
                return new HashSet<int>();

            Func<ISet<int>, ISet<int>> appendDebugDataWhenNeeded = ids =>
            {
                if (logDebugData != null)
                {
                    using (var context = new CustomersContext())
                    {
                        var codes = new List<string> { SearchTermCode.companyNameNormalized.ToString(), SearchTermCode.companyNamePhonetic.ToString() };
                        var result = context
                            .CustomerSearchTerms
                            .Where(x => x.IsActive && ids.Contains(x.CustomerId) && codes.Contains(x.TermCode))
                            .Select(x => new { x.CustomerId, x.TermCode, x.Value })
                            .ToList()
                            .GroupBy(x => x.CustomerId)
                            .ToDictionary(x => x.Key, x => new
                            {
                                name = x.FirstOrDefault(y => y.TermCode == SearchTermCode.companyNameNormalized.ToString())?.Value,
                                tokens = x.Where(y => y.TermCode == SearchTermCode.companyNamePhonetic.ToString()).Select(y => y.Value).ToList()
                            });
                        foreach (var c in result)
                        {
                            logDebugData($"Customer {c.Key}: {c.Value.name}; [{string.Join(", ", c.Value.tokens)}]");
                        }
                    }
                }
                return ids;
            };

            Func<List<SearchHit>, ISet<int>> tweakResult = hits =>
            {
                //Filter out phonetic matches when we have an exact match
                //Note that there is an edgecase where if you search for something you intend to be a prefix and that gets an exact match you wont see the prefix hits. 
                //Unsure if this will be a problem or not. Guessing not and going with this. If it turns out to be wrong, lookup all the actual names when there are both exact matches and phonetic hits and keep an prefix phonetic hits
                if (hits.Count > 1 && hits.Any(x => x.IsExactMatch == 1))
                {
                    var filteredHits = hits
                        .Where(x => x.IsExactMatch == 1)
                        .ToList();

                    if (logDebugData != null)
                    {
                        logDebugData($"Filtered out because of exact match: {string.Join(", ", hits.Select(x => x.CustomerId).Except(filteredHits.Select(x => x.CustomerId)))}");
                    }
                    hits = filteredHits;
                }

                return appendDebugDataWhenNeeded(hits.Select(x => x.CustomerId).ToHashSet());
            };

            var tokens = CompanyLoanSearchTerms.Tokenize(normalizedName);

            if (logDebugData != null)
                logDebugData($"Input: {normalizedName}; [{string.Join(", ", tokens)}]");

            using (var context = new CustomersContext())
            {
                /*
                Intended logic:
                   
                When a single token:
                  Either the customer has a single companyNamePhonetic matching the one token and at most one other companyNamePhonetic rows --at most one instead of zero others to handle things like vattenfall to vattenfall ab
                  Or     the customer has companyNameNormalized that exactly matches the normalized name

                When multiple tokens:
                  Either the customer has at least two companyNamePhonetic rows that match a token
                  Or     the customer has companyNameNormalized that exactly matches the normalized name

                The intention behind all of this is to try and keep the numbers of hits managable while still allowing single word searches
                since there are company names which are just a single word and also always having an exact match work.
                 */
                const string sharedCte =
@"with 
PhoneticNames
as
(
	select	t.CustomerId, 
			t.Value, 
			COUNT(*) as CountInName,
			(select COUNT(*) from @InputPhoneticTerms s where s.Value = t.Value) as CountInSearchTerm
	from	CustomerSearchTerm t
	where	t.TermCode = 'companyNamePhonetic'
	and		t.IsActive = 1
	group by t.CustomerId, t.Value
),
SearchCustomers
as
(
	select	t.CustomerId,
			(select COUNT(*) from PhoneticNames t2 where t2.CustomerId = t.CustomerId) as PhoneticCount,
			max(t.Value) as NormalizedName
	from	CustomerSearchTerm t
	where	t.TermCode = 'companyNameNormalized'
	and		t.IsActive = 1
	group by t.CustomerId
)";
                var inputPhoneticTerms = new DataTable();
                inputPhoneticTerms.Columns.Add("Value", typeof(string));
                foreach (var t in tokens) inputPhoneticTerms.Rows.Add(t);
                var inputPhoneticTermsParam = inputPhoneticTerms.AsTableValuedParameter("[dbo].[NTechSearchTermTVP]");

                if (tokens.Count == 1)
                {
                    var query = sharedCte + " " +
@"select	t.CustomerId, case when t.NormalizedName = @NormalizedName then 1 else 0 end as IsExactMatch
from	SearchCustomers t
where	(
          exists(select 1 from PhoneticNames t2 where t2.CustomerId = t.CustomerId and t2.Value = @TokenValue)
          and 
          (select SUM(t2.CountInName) from PhoneticNames t2 where t2.CustomerId = t.CustomerId) <= 2
        )
or		t.NormalizedName = @NormalizedName";
                    return tweakResult(context.Database.Connection.Query<SearchHit>(query, param: new { @TokenValue = tokens.Single(), NormalizedName = normalizedName, InputPhoneticTerms = inputPhoneticTermsParam }).ToList());
                }
                else
                {
                    var query = sharedCte + " " +
@"select	t.CustomerId, case when t.NormalizedName = @NormalizedName then 1 else 0 end as IsExactMatch
from	SearchCustomers t
where	(select SUM(t2.CountInName) from PhoneticNames t2 where t2.CustomerId = t.CustomerId and t2.Value in (select s.Value from @InputPhoneticTerms s) and t2.CountInName >= t2.CountInSearchTerm) >= 2
or		t.NormalizedName = @NormalizedName";
                    var result = context
                        .Database
                        .Connection
                        .Query<SearchHit>(query, param: new { Values = tokens, NormalizedName = normalizedName, InputPhoneticTerms = inputPhoneticTermsParam })
                        .ToList();

                    return tweakResult(result);
                }
            }
        }

        public void PopulateSearchTerms(NtechCurrentUserMetadata currentUser)
        {
            int[] customerIdsMissingSearchTerms = null;
            if (customerIdsMissingSearchTerms == null)
            {
                using (var context = new CustomersContext())
                {
                    customerIdsMissingSearchTerms = context
                        .CustomerProperties
                        .Where(x =>
                            x.Name == "companyName" && !x.IsEncrypted && x.IsCurrentData
                            && context.CustomerProperties.Any(y => y.CustomerId == x.CustomerId && y.Name == "isCompany" && y.Value == "true" && y.IsCurrentData)
                            && !context.CustomerSearchTerms.Any(y => y.CustomerId == x.CustomerId && y.TermCode == SearchTermCode.companyNameNormalized.ToString() && y.IsActive))
                        .Select(x => x.CustomerId)
                        .ToArray();
                }
            }

            var generator = new Phonix.DoubleMetaphone();
            foreach (var customerIdsGroup in customerIdsMissingSearchTerms.SplitIntoGroupsOfN(500))
            {
                using (var context = new CustomersContext())
                {
                    var idsAndNames = context
                        .CustomerProperties
                        .Where(x =>
                            x.Name == "companyName" && x.IsCurrentData && customerIdsGroup.Contains(x.CustomerId))
                        .Select(x => new { CompanyName = x.Value, x.CustomerId })
                        .ToArray();
                    CompanyLoanSearchTerms.PopulateSearchTermsGroupComposable(currentUser.CoreUser, idsAndNames.Select(x => Tuple.Create(x.CustomerId, x.CompanyName)), context, CoreClock.SharedInstance, generator: generator);

                    context.SaveChanges();
                }
            }
        }
    }

    public interface ICompanyLoanNameSearchService
    {
        void PopulateSearchTerms(NtechCurrentUserMetadata currentUser);
        ISet<int> FindCustomerByCompanyName(string companyName, Action<string> logDebugData = null);
    }
}