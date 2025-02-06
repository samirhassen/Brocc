using Dapper;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class DebtCollectionCandidateService
    {
        private readonly ICoreClock clock;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;

        public DebtCollectionCandidateService(ICoreClock clock, CreditContextFactory creditContextFactory, ICustomerClient customerClient, IClientConfigurationCore clientConfiguration)
        {
            this.clock = clock;
            this.creditContextFactory = creditContextFactory;
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
        }

        private const string DebtCollectionSqlBase =
@"with InitialNotificationAmount
as
(
	select	t.CreditNotificationId, 
			b.EventType,
			t.AccountCode,
			SUM(t.amount) as InitialAmount
	from	AccountTransaction t
	join	BusinessEvent b on t.BusinessEventId = b.Id
	group by t.CreditNotificationId, t.AccountCode, b.EventType
),
OpenNotificationPre
as
(
	select	n.Id,
			n.CreditNr,
			n.DueDate,
			case 
				when @today < n.DueDate then 0
				else DATEDIFF (day, n.[DueDate], @today)
			end as NrOfDaysOverdue,
			case 
				when @today <= n.DueDate then 0
				else 
					(DATEDIFF (month, n.[DueDate], @today)
					 +
					 CASE WHEN ((DATEPART (day, @today)) <= (DATEPART (day, n.[DueDate]))) THEN 0 ELSE 1 END
					)
			end as NrOfPassedDueDatesWithoutFullPaymentSinceNotification,
			RANK() OVER (PARTITION BY n.CreditNr ORDER BY n.DueDate asc) as PerCreditAgeRank,
			isnull((select -sum(nn.InitialAmount) from InitialNotificationAmount nn where nn.CreditNotificationId = n.Id and nn.EventType = 'NewNotification' and nn.AccountCode = 'NotNotifiedCapital'), 0) as InitialCapitalAmount,
			isnull((select sum(nn.InitialAmount) from InitialNotificationAmount nn where nn.CreditNotificationId = n.Id and nn.EventType in ('NewNotification', 'NewReminder') and nn.AccountCode <> 'NotNotifiedCapital'), 0) as InitialNonCapitalAmount,
			isnull((select -sum(tt.amount) from AccountTransaction tt where tt.CreditNotificationId = n.id and tt.IncomingPaymentId is not null and tt.AccountCode = 'CapitalDebt'), 0) as PaidCapitalAmount,
			isnull((select -sum(tt.amount) from AccountTransaction tt where tt.CreditNotificationId = n.id and tt.IncomingPaymentId is not null and tt.AccountCode <> 'CapitalDebt'), 0) as PaidNonCapitalAmount,
			isnull((select sum(tt.amount) from AccountTransaction tt where tt.CreditNotificationId = n.id and tt.WriteoffId is not null and tt.AccountCode = 'NotNotifiedCapital'), 0) as WrittenOffCapitalAmount,
			isnull((select -sum(tt.amount) from AccountTransaction tt where tt.CreditNotificationId = n.id and tt.WriteoffId is not null and tt.AccountCode <> 'NotNotifiedCapital'), 0) as WrittenOffNonCapitalAmount
	from	CreditNotificationHeader n
	where	n.ClosedTransactionDate is null
),
OpenNotification
as
(
	select	p.*,
			p.InitialCapitalAmount + p.InitialNonCapitalAmount as InitialAmount,
			p.InitialCapitalAmount + p.InitialNonCapitalAmount - p.PaidCapitalAmount - p.PaidNonCapitalAmount - p.WrittenOffCapitalAmount - p.WrittenOffNonCapitalAmount as RemainingAmount
	from	OpenNotificationPre p
),
TerminationLetterExt
as
(
	select	t.CreditNr,
			t.DueDate,
			(case when exists(select 1 from CreditNotificationHeader nn where nn.CreditNr = t.CreditNr and nn.DueDate > t.DueDate) then 1 else 0 end) as HasNotificationOverdueAfterTheLetter,
			isnull(t.SuspendsCreditProcess, 0) as SuspendsCreditProcess,
			t.InactivatedByBusinessEventId
	from	CreditTerminationLetterHeader t
),
CreditExt
as
(
	select	h.CreditNr,
			h.[Status] as CreditStatus,
			(select top 1 t.DueDate from TerminationLetterExt t where t.CreditNr = h.CreditNr and (t.SuspendsCreditProcess = 1 or t.HasNotificationOverdueAfterTheLetter = 0) and t.InactivatedByBusinessEventId is null order by t.DueDate desc) as ActualLatestEligableTerminationLetterDueDate,
            dateadd(d, @debtCollectionGraceDays, (select top 1 t.DueDate from TerminationLetterExt t where t.CreditNr = h.CreditNr and (t.SuspendsCreditProcess = 1 or t.HasNotificationOverdueAfterTheLetter = 0) and t.InactivatedByBusinessEventId is null order by t.DueDate desc)) as WithGraceLatestEligableTerminationLetterDueDate,
			(select COUNT(*) from OpenNotification nn where nn.CreditNr = h.CreditNr and nn.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0) as NrUnpaidOverdueNotifications,
			(select top 1 dd.[Value] from DatedCreditDate dd where dd.CreditNr = h.CreditNr and dd.[Name] = 'DebtCollectionPausedUntilDate' and dd.RemovedByBusinessEventId is null order by dd.BusinessEventId desc) as DebtCollectionPostponedUntilDate,
			(select top 1 tt.TransactionDate from AccountTransaction tt where tt.CreditNr = h.CreditNr and not tt.IncomingPaymentId is null order by tt.TransactionDate desc) as LatestPaymentDate,
			(select top 1 s.ExpectedSettlementDate from CreditSettlementOfferHeader s where s.CreditNr = h.CreditNr and s.CancelledByEventId is null and s.CommitedByEventId is null order by s.Id desc) as ExpectedSettlementDate
	from	CreditHeader h
),
DebtCollectionCandiateCreditPre
as
(
	select	c.CreditNr,
			case when 
                (c.DebtCollectionPostponedUntilDate is null or c.DebtCollectionPostponedUntilDate <= @today)
                then 1 else 0 
            end as IsEligableForDebtCollectionExportExceptDate,
			--Nyligen uppskjuten (men inte i innevarande månad)
			case 
				when c.DebtCollectionPostponedUntilDate <  DATEADD(mm, DATEDIFF(m,0,@today), 0)  and DATEDIFF(DAY, c.DebtCollectionPostponedUntilDate, @today) >= 0 and DATEDIFF(DAY, c.DebtCollectionPostponedUntilDate, @today) <= 30 
				then c.DebtCollectionPostponedUntilDate 
				else null 
			end as AttentionWasPostponedUntilDate,
			case
				when c.DebtCollectionPostponedUntilDate > @today then c.DebtCollectionPostponedUntilDate
				else null
			end as ActivePostponedUntilDate,
			c.ActualLatestEligableTerminationLetterDueDate as ActiveTerminationLetterDueDate,
			c.NrUnpaidOverdueNotifications,
			(select top 1 n.NrOfDaysOverdue from OpenNotification n where n.PerCreditAgeRank = 1 and n.CreditNr = c.CreditNr) as NrOfDaysOverdue,
			--Payment after termination letter was sent
			case when c.LatestPaymentDate > c.ActualLatestEligableTerminationLetterDueDate then  c.LatestPaymentDate else null end as AttentionLatestPaymentDateAfterTerminationLetterDueDate,
			c.ExpectedSettlementDate as AttentionSettlementOfferDate,
            c.WithGraceLatestEligableTerminationLetterDueDate
	from	CreditExt c
	where	c.CreditStatus = 'Normal'
	and		not exists(select 1 from OpenNotification nn where nn.CreditNr = c.CreditNr and nn.DueDate > @today)
	and		c.ActualLatestEligableTerminationLetterDueDate <= @today
	and		c.NrUnpaidOverdueNotifications > 0
),
DebtCollectionCandiateCredit
as
(
select	p.*,
		case
			when p.AttentionWasPostponedUntilDate is not null then 1
			when p.AttentionLatestPaymentDateAfterTerminationLetterDueDate is not null then 1
			when p.AttentionSettlementOfferDate is not null then 1
			else 0
		end as HasAttention,
        case when
            p.IsEligableForDebtCollectionExportExceptDate = 1
            and p.WithGraceLatestEligableTerminationLetterDueDate <= @today
        then 1 else 0 end as IsEligableForDebtCollectionExport
from	DebtCollectionCandiateCreditPre p
) ";

        private Dictionary<string, object> GetParameters(Action<Dictionary<string, object>> addExtra = null)
        {
            var p = new Dictionary<string, object>
            {
                { "today", clock.Today },
                { "debtCollectionGraceDays", clientConfiguration.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays") ?? 0 }
            };
            addExtra?.Invoke(p);
            return p;
        }
        public int GetEligibleForDebtCollectionCount(INTechDbContext context) => context.GetConnection().ExecuteScalar<int>(
            DebtCollectionSqlBase + " select count(*) from DebtCollectionCandiateCredit c where c.IsEligableForDebtCollectionExport = 1",
            GetParameters(), commandTimeout: 60);

        public HashSet<string> GetEligibleForDebtCollectionCreditNrs(INTechDbContext context) => context.GetConnection().Query<string>(
                    DebtCollectionSqlBase + " select c.CreditNr from DebtCollectionCandiateCredit c where c.IsEligableForDebtCollectionExport = 1",
                    GetParameters(), commandTimeout: 60)
                .ToHashSetShared();

        public GetDebtCollectionCandidatesPageResult GetDebtCollectionCandidatesPage(string omniSearch, int pageSize, int pageNr, Func<string, string> getCreditUrl)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                List<string> creditNrFilter = null;
                if (!string.IsNullOrWhiteSpace(omniSearch))
                {
                    var o = omniSearch.Trim();
                    if (new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).TryParse(o, out ICivicRegNumber c))
                    {
                        var customerId = customerClient.GetCustomerId(c);

                        creditNrFilter = context.CreditCustomersQueryable.Where(x => x.CustomerId == customerId).Select(y => y.CreditNr).Distinct().ToList();
                        if (creditNrFilter.Count == 0)
                            return new GetDebtCollectionCandidatesPageResult
                            {
                                CurrentPageNr = 0,
                                TotalNrOfPages = 0,
                                Page = new List<GetDebtCollectionCandidatesPageResult.PageItem>()
                            };
                    }
                    else if (o.Any(char.IsDigit))
                    {
                        if (!context.CreditHeadersQueryable.Any(x => x.CreditNr == o))
                            return new GetDebtCollectionCandidatesPageResult
                            {
                                CurrentPageNr = 0,
                                TotalNrOfPages = 0,
                                Page = new List<GetDebtCollectionCandidatesPageResult.PageItem>()
                            };

                        creditNrFilter = new List<string> { o };
                    }
                    else
                    {
                        return new GetDebtCollectionCandidatesPageResult
                        {
                            CurrentPageNr = 0,
                            TotalNrOfPages = 0,
                            Page = new List<GetDebtCollectionCandidatesPageResult.PageItem>()
                        };
                    }
                }

                var q = DebtCollectionSqlBase + " select c.* from DebtCollectionCandiateCredit c ";
                var parameters = GetParameters();
                if (creditNrFilter != null)
                {
                    q = q + " where c.CreditNr in @creditNrs";
                    parameters["creditNrs"] = creditNrFilter;
                }

                var candidates = context.GetConnection().Query<DebtCollectionCandidateModel>(q, parameters, commandTimeout: 120).ToList();
                var totalCount = candidates.Count;

                var tempPage = candidates
                    .Select(x => new DebtColTmp
                    {
                        ActivePostponedUntilDate = x.ActivePostponedUntilDate,
                        HasAttention = x.HasAttention,
                        NrOfDaysOverdue = x.NrOfDaysOverdue,
                        CreditNr = x.CreditNr,
                        NrUnpaidOverdueNotifications = x.NrUnpaidOverdueNotifications,
                        AttentionLatestPaymentDateAfterTerminationLetterDueDate = x.AttentionLatestPaymentDateAfterTerminationLetterDueDate,
                        AttentionWasPostponedUntilDate = x.AttentionWasPostponedUntilDate,
                        AttentionSettlementOfferDate = x.AttentionSettlementOfferDate,
                        IsEligableForDebtCollectionExport = x.IsEligableForDebtCollectionExport,
                        IsEligableForDebtCollectionExportExceptDate = x.IsEligableForDebtCollectionExportExceptDate,
                        ActiveTerminationLetterDueDate = x.ActiveTerminationLetterDueDate,
                        WithGraceLatestEligableTerminationLetterDueDate = x.WithGraceLatestEligableTerminationLetterDueDate
                    })
                    .ToList();

                AppendNotificationBalances(tempPage, context);

                //NOTE: Intentionally fetching all and sorting in memory since the database execution plan is insanely bad for this. Redo the entire sorting so it can be done fast in the db if this becomes a problem
                var currentPage = tempPage
                    .OrderByDescending(x => x.ActivePostponedUntilDate.HasValue ? 0 : 1)
                    .ThenByDescending(x => x.HasAttention ? 1 : 0)
                    .ThenByDescending(x => x.NrOfDaysOverdue)
                    .ThenByDescending(x => x.BalanceAmountUnpaidOverdueNotifications)
                    .ThenByDescending(x => x.CreditNr)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .Select(x => new GetDebtCollectionCandidatesPageResult.PageItem
                    {
                        CreditNr = x.CreditNr,
                        CreditUrl = getCreditUrl(x.CreditNr),
                        NrUnpaidOverdueNotifications = x.NrUnpaidOverdueNotifications,
                        BalanceUnpaidOverdueNotifications = x.BalanceAmountUnpaidOverdueNotifications,
                        InitialUnpaidOverdueNotifications = x.InitialAmountUnpaidOverdueNotifications,
                        FractionBalanceUnpaidOverdueNotifications = x.InitialAmountUnpaidOverdueNotifications <= 0m ? 0m : (x.BalanceAmountUnpaidOverdueNotifications / x.InitialAmountUnpaidOverdueNotifications),
                        NrOfDaysOverdue = x.NrOfDaysOverdue,
                        HasAttention = x.HasAttention,
                        AttentionLatestPaymentDateAfterTerminationLetterDueDate = x.AttentionLatestPaymentDateAfterTerminationLetterDueDate,
                        AttentionWasPostponedUntilDate = x.AttentionWasPostponedUntilDate,
                        AttentionSettlementOfferDate = x.AttentionSettlementOfferDate,
                        IsEligableForDebtCollectionExport = x.IsEligableForDebtCollectionExport,
                        IsEligableForDebtCollectionExportExceptDate = x.IsEligableForDebtCollectionExportExceptDate,
                        ActivePostponedUntilDate = x.ActivePostponedUntilDate,
                        ActiveTerminationLetterDueDate = x.ActiveTerminationLetterDueDate,
                        WithGraceLatestEligableTerminationLetterDueDate = x.WithGraceLatestEligableTerminationLetterDueDate
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return new GetDebtCollectionCandidatesPageResult
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                };
            }
        }
        public class GetDebtCollectionCandidatesPageResult
        {
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public List<PageItem> Page { get; set; }
            public class PageItem
            {
                public string CreditNr { get; set; }
                public string CreditUrl { get; set; }
                public int NrUnpaidOverdueNotifications { get; set; }
                public decimal BalanceUnpaidOverdueNotifications { get; set; }
                public decimal InitialUnpaidOverdueNotifications { get; set; }
                public decimal FractionBalanceUnpaidOverdueNotifications { get; set; }
                public int NrOfDaysOverdue { get; set; }
                public bool HasAttention { get; set; }
                public DateTime? AttentionLatestPaymentDateAfterTerminationLetterDueDate { get; set; }
                public DateTime? AttentionWasPostponedUntilDate { get; set; }
                public DateTime? AttentionSettlementOfferDate { get; set; }
                public bool IsEligableForDebtCollectionExport { get; set; }
                public bool IsEligableForDebtCollectionExportExceptDate { get; set; }
                public DateTime? ActivePostponedUntilDate { get; set; }
                public DateTime? ActiveTerminationLetterDueDate { get; set; }
                public DateTime? WithGraceLatestEligableTerminationLetterDueDate { get; set; }
            }
        }

        private class OpenNotificationModel
        {
            public string CreditNr { get; set; }
            public DateTime DueDate { get; set; }
            public int NrOfPassedDueDatesWithoutFullPaymentSinceNotification { get; set; }
            public int NrOfDaysOverdue { get; set; }
        }

        private class DebtColTmp
        {
            public DateTime? ActivePostponedUntilDate { get; internal set; }
            public bool HasAttention { get; internal set; }
            public int NrOfDaysOverdue { get; internal set; }
            public decimal BalanceAmountUnpaidOverdueNotifications { get; internal set; }
            public string CreditNr { get; internal set; }
            public int NrUnpaidOverdueNotifications { get; internal set; }
            public decimal InitialAmountUnpaidOverdueNotifications { get; internal set; }
            public DateTime? AttentionLatestPaymentDateAfterTerminationLetterDueDate { get; internal set; }
            public DateTime? AttentionWasPostponedUntilDate { get; internal set; }
            public DateTime? AttentionSettlementOfferDate { get; internal set; }
            public bool IsEligableForDebtCollectionExport { get; internal set; }
            public bool IsEligableForDebtCollectionExportExceptDate { get; set; }
            public DateTime? ActiveTerminationLetterDueDate { get; internal set; }
            public DateTime? WithGraceLatestEligableTerminationLetterDueDate { get; set; }
        }

        private class NotificationBalanceData
        {
            public decimal InitialAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string CreditNr { get; set; }
        }

        private void AppendNotificationBalances(IList<DebtColTmp> items, ICreditContextExtended context)
        {
            //We fetch balances separately since the database fails miserably on doing it at the same time
            //Total runtime serially ~1 second. Total runtime doing it at the same time ~90 seconds.
            var creditNrs = items.Select(x => x.CreditNr).ToArray();

            var dbConnection = context.GetConnection();

            var query = DebtCollectionSqlBase + @" select  n.CreditNr, 
		sum(n.InitialAmount) as InitialAmount,
		sum(n.RemainingAmount) as RemainingAmount
from	OpenNotification n
where	n.CreditNr in @creditNrs
and		n.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0
group by n.CreditNr";

            var notificationBalances = creditNrs
                .SplitIntoGroupsOfN(200)
                .SelectMany(x => dbConnection.Query<NotificationBalanceData>(query, GetParameters(p => p["creditNrs"] = x), commandTimeout: 120).ToList())
                .ToDictionary(x => x.CreditNr);

            foreach (var i in items)
            {
                var n = notificationBalances.Opt(i.CreditNr);
                i.InitialAmountUnpaidOverdueNotifications = n?.InitialAmount ?? 0m;
                i.BalanceAmountUnpaidOverdueNotifications = n?.RemainingAmount ?? 0m;
            }
        }

        private class DebtCollectionCandidateModelCount
        {
            public string CreditNr { get; set; }
            public bool IsEligableForDebtCollectionExport { get; set; }
        }

        private class DebtCollectionCandidateModel : DebtCollectionCandidateModelCount
        {
            public int NrUnpaidOverdueNotifications { get; set; }
            public int NrOfDaysOverdue { get; set; }
            public bool HasAttention { get; set; }
            public DateTime? AttentionWasPostponedUntilDate { get; set; }
            public DateTime? ActiveTerminationLetterDueDate { get; set; }
            public DateTime? AttentionLatestPaymentDateAfterTerminationLetterDueDate { get; set; }
            public DateTime? AttentionSettlementOfferDate { get; set; }
            public DateTime? ActivePostponedUntilDate { get; set; }
            public DateTime? WithGraceLatestEligableTerminationLetterDueDate { get; set; }
            public bool IsEligableForDebtCollectionExportExceptDate { get; set; }
        }
    }
}