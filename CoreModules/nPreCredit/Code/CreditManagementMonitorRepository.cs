using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nPreCredit.Code
{
    public class CreditManagementMonitorRepository
    {
        private IClock clock;

        public CreditManagementMonitorRepository(IClock clock)
        {
            this.clock = clock;
        }

        public class Provider
        {
            public string ProviderName { get; set; }
            public string DisplayName { get; set; }
        }

        public class MonitorCategoryItem
        {
            public string CategoryCode { get; set; }
            public int CategoryCount { get; set; }
            public string ExampleApplicationNr { get; set; }
        }

        public class RejectionReasonItem
        {
            public string CategoryCode { get; set; }
            public string RejectionReasonName { get; set; }
            public int NrOfRejections { get; set; }
        }

        public class RejectionReasonBreakdownItem
        {
            public string RejectionReasonName { get; set; }
            public decimal Percent { get; set; }
        }

        public class ProviderItem
        {
            public string ProviderName { get; set; }
            public int NrOfApplications { get; set; }
            public int NrOfAcceptedApplications { get; set; }
            public decimal PercentAcceptedApplications { get; set; }
        }

        public class MonitorDataSet
        {
            public int TotalCount { get; set; }
            public decimal PercentBaseCount { get; set; }
            public List<Category> Categories { get; set; }
            public DetailsSet Details { get; set; }

            public class Category
            {
                public string CategoryCode { get; set; }
                public int Count { get; set; }
                public decimal? Percent { get; set; }
            }
            public class DetailsSet
            {
                public List<RejectionReasonBreakdownItem> TopAutoRejectionPercents { get; set; }
                public List<RejectionReasonBreakdownItem> TopManualRejectionPercents { get; set; }
                public List<ProviderItem> ApplicationCountPerProvider { get; set; }
            }
        }

        public List<Provider> GetProviders()
        {
            return NTechCache.WithCache("772b5926-e3e6-4e3a-a0a0-53235c038c04", TimeSpan.FromHours(1), () =>
            {
                using (var context = new PreCreditContext())
                {
                    var providerNames = context.CreditApplicationHeaders.Select(x => x.ProviderName).Distinct().ToList();

                    return providerNames.Select(x =>
                    {
                        var a = NEnv.GetAffiliateModel(x, allowMissing: true);
                        return a == null ? new Provider { DisplayName = x, ProviderName = x } : new Provider { ProviderName = x, DisplayName = a.DisplayToEnduserName };
                    }).ToList();
                }
            });
        }

        public class MonitorApplicationModel
        {
            public string ApplicationNr { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public string ProviderName { get; set; }
            public string CategoryCode { get; set; }
            public int? CurrentCreditDecisionId { get; set; }
            public ISet<string> RejectionReasons { get; set; }
        }

        private class ApplicationRejectionReasonModel
        {
            public string TermValue { get; set; }
            public int CreditDecisionId { get; set; }
        }

        public bool TryGetMonitorDataDetails(string providerName, string timeSpan, out string errorMessage, out List<MonitorApplicationModel> applications)
        {
            errorMessage = null;
            applications = null;

            if (!IsValidFilters(providerName, timeSpan, out errorMessage))
                return false;

            var now = this.clock.Now;
            using (var context = new PreCreditContext())
            {
                const string RejectionReansonSqlPattern =
@"select	r.TermValue, r.CreditDecisionId
from	CreditDecisionSearchTerm r
where	r.TermName = 'RejectionReason'
and		r.CreditDecisionId in({0})";
                Func<List<int>, List<ApplicationRejectionReasonModel>> getTermsBatch = ids =>
                    ids.Count == 0
                        ? new List<ApplicationRejectionReasonModel>()
                        : context.Database.SqlQuery<ApplicationRejectionReasonModel>(string.Format(RejectionReansonSqlPattern, string.Join(",", ids))).ToList();

                var filter = CreateFilter(now, providerName, timeSpan);

                const string ApplicationsSqlPattern =
@"select    t.ApplicationNr, t.ApplicationDate, t.ProviderName, t.CategoryCode, t.CurrentCreditDecisionId
from	    Tmp2 t
where	1=1
{0}";
                var applicationsSql = string.Format(SqlPatternBase + ApplicationsSqlPattern, filter.Item1);
                applications = context
                    .Database
                    .SqlQuery<MonitorApplicationModel>(applicationsSql, filter.Item2.ToArray())
                    .ToList();
                var appsByDecisionId = applications.Where(x => x.CurrentCreditDecisionId.HasValue).ToDictionary(x => x.CurrentCreditDecisionId.Value);

                foreach (var nrGroup in applications.Where(x => x.CurrentCreditDecisionId.HasValue).Select(x => x.CurrentCreditDecisionId.Value).ToArray().SplitIntoGroupsOfN(500))
                {
                    foreach (var t in getTermsBatch(nrGroup.ToList()))
                    {
                        var a = appsByDecisionId[t.CreditDecisionId];
                        if (a.RejectionReasons == null) a.RejectionReasons = new HashSet<string>();
                        a.RejectionReasons.Add(t.TermValue);
                    }
                }

                return true;
            }
        }

        public bool TryGetMonitorData(string providerName, string timeSpan, bool? includeDetails, int? nrOfAutoRejectionReasonsToShow, int? nrOfManualRejectionReasonsToShow, int? nrOfProviderItemsToShow, out MonitorDataSet result, out string errorMessage)
        {
            errorMessage = null;
            result = null;

            if (!IsValidFilters(providerName, timeSpan, out errorMessage))
                return false;

            var now = this.clock.Now;
            Func<Tuple<string, List<SqlParameter>>> createFilter = () => this.CreateFilter(now, providerName, timeSpan);

            var pHelper = new ReportingPercentageHelper();

            using (var context = new PreCreditContext())
            {
                var countsFilter = createFilter();
                var counts = context
                    .Database
                    .SqlQuery<MonitorCategoryItem>(string.Format(SqlPatternBase + SqlPatternOverview, countsFilter.Item1), countsFilter.Item2.ToArray())
                    .ToList();
                var totalCount = counts.Aggregate(0, (acc, x) => acc + x.CategoryCount);

                var categoriesWithPercent = counts
                    .Where(x => x.CategoryCode.IsOneOf("AutoAccepted", "ManuallyAccepted", "AutoRejected", "ManuallyRejected"))
                    .ToList();
                var percentBaseCount = categoriesWithPercent.Aggregate(0, (acc, x) => acc + x.CategoryCount);

                var categoryPercents = new Dictionary<string, decimal>();
                pHelper.GetRoundedListThatSumsCorrectly(
                    categoriesWithPercent,
                    x => percentBaseCount == 0 ? 0m : 100m * ((decimal)x.CategoryCount / (decimal)percentBaseCount),
                    (a, b) => categoryPercents[a.CategoryCode] = b,
                    0);

                Func<MonitorDataSet.DetailsSet> createDetails = () =>
                {
                    if (!(includeDetails ?? false))
                        return null;

                    var rejectionsFilter = createFilter();
                    var rejectionPattern = string.Format(SqlPatternBase + SqlPatternRejectionBreakdown, rejectionsFilter.Item1);
                    var rejectionReasons = context
                        .Database
                        .SqlQuery<RejectionReasonItem>(rejectionPattern, rejectionsFilter.Item2.ToArray())
                        .ToList();
                    var topAutoRejectionPercents = GetTopRejectionReasonBreakdown("AutoRejected", counts, rejectionReasons, pHelper, nrOfAutoRejectionReasonsToShow ?? 5);
                    var topManualRejectionPercents = GetTopRejectionReasonBreakdown("ManuallyRejected", counts, rejectionReasons, pHelper, nrOfManualRejectionReasonsToShow ?? 5);

                    var providerFilter = createFilter();
                    var providerItems = context
                        .Database
                        .SqlQuery<ProviderItem>(string.Format(SqlPatternBase + SqlPatternProviderBreakdown, providerFilter.Item1), providerFilter.Item2.ToArray())
                        .ToList()
                        .OrderByDescending(x => x.NrOfApplications)
                        .Take(nrOfProviderItemsToShow ?? 5)
                        .ToList();

                    foreach (var p in providerItems)
                        p.PercentAcceptedApplications = p.NrOfApplications == 0
                            ? 0m
                            : Math.Round(100m * ((decimal)p.NrOfAcceptedApplications / (decimal)p.NrOfApplications), 1);

                    return new MonitorDataSet.DetailsSet
                    {
                        TopAutoRejectionPercents = topAutoRejectionPercents,
                        TopManualRejectionPercents = topManualRejectionPercents,
                        ApplicationCountPerProvider = providerItems
                    };
                };

                result = new MonitorDataSet
                {
                    TotalCount = totalCount,
                    PercentBaseCount = percentBaseCount,
                    Categories = counts.Select(x => new MonitorDataSet.Category
                    {
                        CategoryCode = x.CategoryCode,
                        Count = x.CategoryCount,
                        Percent = categoryPercents.ContainsKey(x.CategoryCode) ? categoryPercents[x.CategoryCode] : new decimal?()
                    }).ToList(),
                    Details = createDetails()
                };

                return true;
            }
        }

        private Tuple<string, List<SqlParameter>> CreateFilter(DateTimeOffset now, string providerName, string timeSpan)
        {
            List<SqlParameter> parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("@now", now));
            string filter = "";

            if (providerName != "*")
            {
                filter += " and t.ProviderName = @providerName ";
                parameters.Add(new SqlParameter("@providerName", providerName));
            }

            var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, 0, now.Offset);
            if (timeSpan == "today")
            {
                filter += " and t.ApplicationDate >= @today ";
                parameters.Add(new SqlParameter("@today", today));
            }
            else if (timeSpan == "yesterday")
            {
                var yesterday = today.AddDays(-1);
                filter += " and t.ApplicationDate >= @yesterday and t.ApplicationDate < @today ";
                parameters.Add(new SqlParameter("@today", today));
                parameters.Add(new SqlParameter("@yesterday", yesterday));
            }
            else if (timeSpan == "thisweek")
            {
                var firstDayOfWeek = FirstDayOfWeek(today, DayOfWeek.Monday);
                filter += " and t.ApplicationDate >= @firstDayOfWeek ";
                parameters.Add(new SqlParameter("@firstDayOfWeek", firstDayOfWeek));
            }
            else if (timeSpan == "thismonth")
            {
                var firstOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, 0, now.Offset);
                filter += " and t.ApplicationDate >= @firstOfMonth ";
                parameters.Add(new SqlParameter("@firstOfMonth", firstOfMonth));
            }
            else
                throw new NotImplementedException();

            return Tuple.Create(filter, parameters);
        }

        private bool IsValidFilters(string providerName, string timeSpan, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(providerName))
            {
                errorMessage = "Missing providerName";
                return false;
            }

            if (!timeSpan.IsOneOf("today", "yesterday", "thisweek", "thismonth"))
            {
                errorMessage = "Invalid or missing timeSpan";
                return false;
            }

            return true;
        }

        private List<RejectionReasonBreakdownItem> GetTopRejectionReasonBreakdown(string categoryCode, List<MonitorCategoryItem> counts, List<RejectionReasonItem> rejectionReasons, ReportingPercentageHelper pHelper, int topReasonsCount)
        {
            var autoRejectionCount = counts
                .Where(x => x.CategoryCode == categoryCode)
                .Aggregate(0, (acc, x) => acc + x.CategoryCount);
            var autoRejectionReasons = rejectionReasons
                .Where(x => x.CategoryCode == categoryCode)
                .ToList();
            var autoRejectionPercents = new Dictionary<string, decimal>();
            pHelper.GetRoundedListThatSumsCorrectly(
                autoRejectionReasons,
                x => autoRejectionCount == 0 ? 0m : 100m * ((decimal)x.NrOfRejections / (decimal)autoRejectionCount),
                (a, b) => autoRejectionPercents[a.RejectionReasonName] = b,
                1);
            return autoRejectionPercents
                    .OrderByDescending(x => x.Value)
                    .Take(topReasonsCount)
                    .Select(x => new RejectionReasonBreakdownItem { RejectionReasonName = x.Key, Percent = x.Value })
                    .ToList();
        }

        private static DateTimeOffset FirstDayOfWeek(DateTimeOffset dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(-1 * diff);
        }

        const string SqlPatternBase = @"with Tmp
as
(
	select	h.ApplicationNr,
			h.ProviderName,
			d.WasAutomated,
			d.Discriminator,
			h.IsCancelled,
			h.ApplicationDate,
            h.CurrentCreditDecisionId
	from	CreditApplicationHeader h
	left outer join CreditDecision d on h.CurrentCreditDecisionId = d.Id
	where	(h.HideFromManualListsUntilDate is null or h.HideFromManualListsUntilDate < @now)
),
Tmp2
as
(
	select	t.ApplicationNr,
			t.ApplicationDate,
            t.ProviderName,
			case 
				when t.Discriminator = 'AcceptedCreditDecision' and t.WasAutomated = 1 then 'AutoAccepted'
				when t.Discriminator = 'AcceptedCreditDecision' and t.WasAutomated <> 1 then 'ManuallyAccepted'
				when t.Discriminator = 'RejectedCreditDecision' and t.WasAutomated = 1 then 'AutoRejected'
				when t.Discriminator = 'RejectedCreditDecision' and t.WasAutomated <> 1 then 'ManuallyRejected'
				when t.IsCancelled = 1 then 'Cancelled'
				else 'Manual'
			end as CategoryCode,
            t.CurrentCreditDecisionId
	from	Tmp t
)";

        const string SqlPatternOverview =
@"select    	t.CategoryCode, COUNT(*) as CategoryCount, MIN(t.ApplicationNr) as ExampleApplicationNr
from	    Tmp2 t
where	1=1
{0}
group by t.CategoryCode";

        const string SqlPatternRejectionBreakdown =
@",FilteredSet
as
(
	select	*
	from	Tmp2 t
	where	1=1
	{0}
)
select	t.CategoryCode, r.TermValue as RejectionReasonName, COUNT(*) as NrOfRejections
from	CreditDecisionSearchTerm r
join    FilteredSet t on t.CurrentCreditDecisionId = r.CreditDecisionId
where	r.TermName = 'RejectionReason'
group by t.CategoryCode, r.TermValue";

        const string SqlPatternProviderBreakdown =
@"select	    t.ProviderName, 
            COUNT(*) as NrOfApplications,
            SUM(case when t.CategoryCode = 'AutoAccepted' then 1 when t.CategoryCode = 'ManuallyAccepted' then 1 else 0 end) as NrOfAcceptedApplications
 from   Tmp2 t
 where	1=1
 {0}
 group by t.ProviderName";
    }
}