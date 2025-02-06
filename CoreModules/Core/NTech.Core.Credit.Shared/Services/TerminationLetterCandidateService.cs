using Dapper;
using nCredit.DbModel.DomainModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace nCredit.Code.Services
{
    public class TerminationLetterCandidateService
    {
        private readonly ICoreClock clock;
        private readonly DebtCollectionCandidateService debtCollectionCandidateService;
        private readonly ICreditEnvSettings creditEnvSettings;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly Lazy<NotificationProcessSettings> notificationProcessSettings;

        public TerminationLetterCandidateService(ICoreClock clock, DebtCollectionCandidateService debtCollectionCandidateService, INotificationProcessSettingsFactory notificationProcessSettingsFactory,
            ICreditEnvSettings creditEnvSettings, CreditContextFactory creditContextFactory, ICustomerClient customerClient, IClientConfigurationCore clientConfiguration)
        {
            this.clock = clock;
            this.debtCollectionCandidateService = debtCollectionCandidateService;
            this.creditEnvSettings = creditEnvSettings;
            this.creditContextFactory = creditContextFactory;
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
            notificationProcessSettings = new Lazy<NotificationProcessSettings>(() => notificationProcessSettingsFactory.GetByCreditType(creditEnvSettings.ClientCreditType));
        }

        private const string TerminationLetterQueryBase = @"with 
InitialNotificationAmount
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
            dateadd(d, 1, dateadd(m, 2, n.DueDate)) as TerminationPreviewDate,
            dateadd(d, 1 + @terminationGraceDays, dateadd(m, 2, n.DueDate)) as TerminationCandidateDate,
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
TerminationLetterCandidateCreditPre1
as
(
	select	h.CreditNr,
			(select top 1 t.DueDate from CreditTerminationLetterHeader t where t.CreditNr = h.CreditNr and t.InactivatedByBusinessEventId is null order by t.DueDate desc) as LatestTerminationLetterDueDate,
			(select top 1 t.DueDate from CreditTerminationLetterHeader t where t.CreditNr = h.CreditNr order by t.DueDate desc) as LatestTerminationLetterDueDateEvenIfInactivated,
			isnull((select top 1 n.NrOfPassedDueDatesWithoutFullPaymentSinceNotification from OpenNotification n where n.PerCreditAgeRank = 1 and n.CreditNr = h.CreditNr), 0) as NrOfPassedDueDatesWithoutFullPayment,
            (select top 1 n.TerminationCandidateDate from OpenNotification n where n.PerCreditAgeRank = 1 and n.CreditNr = h.CreditNr) as TerminationCandidateDate,
            (select top 1 n.TerminationPreviewDate from OpenNotification n where n.PerCreditAgeRank = 1 and n.CreditNr = h.CreditNr) as TerminationPreviewDate,
			isnull((select min(n.RemainingAmount) from OpenNotification n where n.CreditNr = h.CreditNr), 0) as LowestOpenNotificationBalance,
			(select top 1 dd.[Value] from DatedCreditDate dd where dd.CreditNr = h.CreditNr and dd.[Name] = 'TerminationLettersPausedUntilDate' order by dd.BusinessEventId desc) as TerminationLettersPostponedUntilDate,
			(select top 1 dd.[Value] from DatedCreditDate dd where dd.CreditNr = h.CreditNr and dd.[Name] = 'PromisedToPayDate' and dd.RemovedByBusinessEventId is null order by dd.BusinessEventId desc) as PromisedToPayDate,
			(select top 1 s.ExpectedSettlementDate from CreditSettlementOfferHeader s where s.CreditNr = h.CreditNr and s.CancelledByEventId is null and s.CommitedByEventId is null order by s.Id desc) as ExpectedSettlementDate,
			case 
				when exists(select 1 from CreditTerminationLetterHeader t where t.CreditNr = h.CreditNr and t.SuspendsCreditProcess = 1 and t.InactivatedByBusinessEventId is null)
				then 1 
				else 0 
			end as IsCreditProcessSuspendedByTerminationLetter,
            (select top 1 dd.[Value] from DatedCreditString dd where dd.CreditNr = h.CreditNr and dd.[Name] = 'IsStandardDefaultProcessSuspended' order by dd.BusinessEventId desc) as IsStandardDefaultProcessSuspended
	from	CreditHeader h
	where	h.[Status] = 'Normal'
	and		not h.CreditNr in (select d.CreditNr from @debtCollectionCreditNrs d)
	and		h.CreditType = @creditType
),
TerminationLetterCandidateCreditPre2
as
(
	select	p.*,
			case when p.LatestTerminationLetterDueDate > @today then p.LatestTerminationLetterDueDate else null end as ActiveTerminationLetterDueDate,
			case  --Varit uppsagd nyligen
				when datediff(day, p.LatestTerminationLetterDueDateEvenIfInactivated, @today) >= 0 and  datediff(day, p.LatestTerminationLetterDueDateEvenIfInactivated, @today) <= 30 
				then p.LatestTerminationLetterDueDateEvenIfInactivated 
				else null 
			end as AttentionHasRecentOverdueTerminationLetter,
			isnull((select sum(nn.RemainingAmount) from OpenNotification nn where nn.CreditNr = p.CreditNr and nn.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0), 0) as BalanceAmountUnpaidOverdueNotifications,
			isnull((select sum(nn.InitialAmount) from OpenNotification nn where nn.CreditNr = p.CreditNr and nn.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0), 0) as InitialAmountUnpaidOverdueNotifications,
			(select count(*) from OpenNotification nn where nn.CreditNr = p.CreditNr and nn.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0) as NrUnpaidOverdueNotifications,
			isnull((select top 1 n.NrOfDaysOverdue from OpenNotification n where n.PerCreditAgeRank = 1 and n.CreditNr = p.CreditNr), 0) as NrOfDaysOverdue,
			case --Låg balans på förfallen avi
				when p.LowestOpenNotificationBalance <= @lowBalanceLimit and p.NrOfPassedDueDatesWithoutFullPayment <= 3
				then p.LowestOpenNotificationBalance
				else null
			end as AttentionNotificationLowBalanceAmount,
			case --Nyligen uppskjuten (men inte i innevarande månad)
				when p.TerminationLettersPostponedUntilDate <  DATEADD(mm, DATEDIFF(m,0,@today), 0)  and DATEDIFF(DAY, p.TerminationLettersPostponedUntilDate, @today) >= 0 and DATEDIFF(DAY, p.TerminationLettersPostponedUntilDate, @today) <= 30 
				then p.TerminationLettersPostponedUntilDate 
				else null 
			end as AttentionWasPostponedUntilDate,
			case 
				when p.PromisedToPayDate > dateadd(d, -5, @today)
				then p.PromisedToPayDate
				else null
			end as AttentionPromisedToPayDateRecentOrFuture,
			p.ExpectedSettlementDate as AttentionSettlementOfferDate,
			case
				when p.TerminationLettersPostponedUntilDate > @today then p.TerminationLettersPostponedUntilDate
				else null
			end as ActivePostponedUntilDate
	from	TerminationLetterCandidateCreditPre1 p
),
TerminationLetterCandidateCreditPre3
as
(
	select	p.*,
			case 
				when p.AttentionNotificationLowBalanceAmount is not null then 1
				when p.AttentionWasPostponedUntilDate is not null then 1
				when p.AttentionHasRecentOverdueTerminationLetter is not null then 1
				when p.AttentionPromisedToPayDateRecentOrFuture is not null then 1
				when p.AttentionSettlementOfferDate is not null then 1
				else 0
			end as HasAttention,
			case 
				when 
					p.ActiveTerminationLetterDueDate is null 
					and p.ActivePostponedUntilDate is null
					and p.IsCreditProcessSuspendedByTerminationLetter = 0
                    and isnull(p.IsStandardDefaultProcessSuspended, 'false') = 'false'
				then 1 else 0
			end as IsEligableForTerminationLetterExpectDate
	from	TerminationLetterCandidateCreditPre2 p
), 
TerminationLetterCandidateCredit
as
(
    select	p.*,
			case 
				when p.TerminationCandidateDate <= @today
					 and p.IsEligableForTerminationLetterExpectDate = 1
				then 1 else 0
			end as IsEligableForTerminationLetter
    from    TerminationLetterCandidateCreditPre3 p
) ";

        private T[] Query<T>(INTechDbContext context, string query, Dictionary<string, object> additionalParameters = null)
        {
            var lowNotificationBalanceLimit = notificationProcessSettings.Value.GetMaxTotalReminderFeePerNotification();
            var debtCollectionCreditNrs = debtCollectionCandidateService.GetEligibleForDebtCollectionCreditNrs(context);

            //Performance optimization. Just doing the query as in @creditNrs and setting the list as parameter works fine up to like 50 or so but when 2000 are under debt col in blow up miserably.
            //Would probably still be ok but a bit low for comfort.
            var debtCollectionCreditNrsTable = new DataTable();
            debtCollectionCreditNrsTable.Columns.Add(new DataColumn("CreditNr", typeof(string)));
            foreach (var creditNr in debtCollectionCreditNrs)
            {
                debtCollectionCreditNrsTable.Rows.Add(creditNr);
            }            
            var parameters = new Dictionary<string, object>
            {
                { "creditType", creditEnvSettings.ClientCreditType.ToString()},
                { "today", clock.Today },
                { "lowBalanceLimit", lowNotificationBalanceLimit },
                { "debtCollectionCreditNrs", debtCollectionCreditNrsTable.AsTableValuedParameter("dbo.CreditNrs") },
                { "terminationGraceDays", clientConfiguration.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays") ?? 0 }
            };
            if (additionalParameters != null)
            {
                foreach (var p in additionalParameters)
                {
                    parameters[p.Key] = p.Value;
                }
            }

            var connection = context.GetConnection();
            connection.Execute("IF TYPE_ID(N'dbo.CreditNrs') IS NULL CREATE TYPE dbo.CreditNrs AS TABLE(CreditNr nvarchar(128) NOT NULL)");
            return context.GetConnection().Query<T>(TerminationLetterQueryBase + " " + query, param: parameters, commandTimeout: 120).ToArray();
        }

        //TODO: Do we expand this list to also include any active credits within the same MortgageLoanAgreementNr even if some of them are not in the list originally?
        public string[] GetEligibleForTerminationLetterCreditNrs(INTechDbContext context) =>
            Query<string>(context, "select c.CreditNr from TerminationLetterCandidateCredit c where c.IsEligableForTerminationLetter = 1");

        public int GetEligableForTerminationLettersCount(INTechDbContext context) =>
            Query<int>(context, "select count(*) from TerminationLetterCandidateCredit c where c.IsEligableForTerminationLetter = 1").Single();

        public GetTerminationLetterCandidatesPageResult GetTerminationLetterCandidatesPage(int pageSize, int pageNr, string omniSearch, Func<string, string> getCreditUrl)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var query = @"select c.* from TerminationLetterCandidateCredit c 
                                  where ((c.IsEligableForTerminationLetterExpectDate = 1 and c.TerminationPreviewDate <= @today) or c.ActiveTerminationLetterDueDate is not null or c.ActivePostponedUntilDate is not null) ";

                Dictionary<string, object> additionalParameters = null;
                if (!string.IsNullOrWhiteSpace(omniSearch))
                {
                    var o = omniSearch.Trim();
                    if (new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).TryParse(o, out ICivicRegNumber c))
                    {
                        var customerId = customerClient.GetCustomerId(c);

                        var creditNrs = context.CreditCustomersQueryable.Where(x => x.CustomerId == customerId).Select(x => x.CreditNr).Distinct().ToList();
                        if (creditNrs.Count == 0)
                        {
                            return new GetTerminationLetterCandidatesPageResult
                            {
                                CurrentPageNr = 0,
                                TotalNrOfPages = 0,
                                Page = new List<GetTerminationLetterCandidatesPageResult.PageItem>()
                            };
                        }

                        additionalParameters = new Dictionary<string, object> { { "searchCreditNrs", creditNrs } };
                        query += " and c.CreditNr in @searchCreditNrs";
                    }
                    else if (o.Any(char.IsDigit))
                    {
                        if (!context.CreditHeadersQueryable.Any(x => x.CreditNr == o))
                        {
                            return new GetTerminationLetterCandidatesPageResult
                            {
                                CurrentPageNr = 0,
                                TotalNrOfPages = 0,
                                Page = new List<GetTerminationLetterCandidatesPageResult.PageItem>()
                            };
                        }

                        additionalParameters = new Dictionary<string, object> { { "searchCreditNr", o } };
                        query += " and c.CreditNr = @searchCreditNr";
                    }
                    else
                    {
                        return new GetTerminationLetterCandidatesPageResult
                        {
                            CurrentPageNr = 0,
                            TotalNrOfPages = 0,
                            Page = new List<GetTerminationLetterCandidatesPageResult.PageItem>()
                        };
                    }
                }

                var baseResult = Query<TerminationLetterCandidateModel>(context, query, additionalParameters: additionalParameters);

                var totalCount = baseResult.Length;

                var currentPage = baseResult
                    .ToList()
                    //TODO: This comment was true before moving to the sql based solution. Might be not the case any more. Benchmark and find out!
                    //NOTE: Intentionally fetching all and sorting in memory since the database execution plan is insanely bad for this. Redo the entire sorting so it can be done fast in the db if this becomes a problem
                    .OrderByDescending(x => x.ActiveTerminationLetterDueDate.HasValue ? 1 : (!x.HasAttention ? 2 : 3))
                    .ThenByDescending(x => x.CreditNr)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .Select(x => new GetTerminationLetterCandidatesPageResult.PageItem
                    {
                        CreditNr = x.CreditNr,
                        CreditUrl = getCreditUrl(x.CreditNr),
                        NrUnpaidOverdueNotifications = x.NrUnpaidOverdueNotifications,
                        BalanceUnpaidOverdueNotifications = x.BalanceAmountUnpaidOverdueNotifications,
                        InitialUnpaidOverdueNotifications = x.InitialAmountUnpaidOverdueNotifications,
                        FractionBalanceUnpaidOverdueNotifications = x.InitialAmountUnpaidOverdueNotifications <= 0m ? 0m : (x.BalanceAmountUnpaidOverdueNotifications / x.InitialAmountUnpaidOverdueNotifications),
                        NrOfDaysOverdue = x.NrOfDaysOverdue,
                        HasAttention = x.HasAttention,
                        AttentionHasRecentOverdueTerminationLetter = x.AttentionHasRecentOverdueTerminationLetter,
                        AttentionNotificationLowBalanceAmount = x.AttentionNotificationLowBalanceAmount,
                        AttentionTotalLowBalanceAmount = x.AttentionTotalLowBalanceAmount,
                        AttentionWasPostponedUntilDate = x.AttentionWasPostponedUntilDate,
                        AttentionPromisedToPayDateRecentOrFuture = x.AttentionPromisedToPayDateRecentOrFuture,
                        IsEligableForTerminationLetter = x.IsEligableForTerminationLetter,
                        ActivePostponedUntilDate = x.ActivePostponedUntilDate,
                        ActiveTerminationLetterDueDate = x.ActiveTerminationLetterDueDate,
                        AttentionSettlementOfferDate = x.AttentionSettlementOfferDate,
                        TerminationCandidateDate = x.TerminationCandidateDate,
                        TerminationPreviewDate = x.TerminationPreviewDate,
                        IsEligableForTerminationLetterExpectDate = x.IsEligableForTerminationLetterExpectDate
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return new GetTerminationLetterCandidatesPageResult
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                };
            }
        }

        public DateTime? GetTerminationCandidateDate(string creditNr)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var query = @"select c.TerminationCandidateDate from TerminationLetterCandidateCredit c 
                                  where ((c.IsEligableForTerminationLetterExpectDate = 1 and c.TerminationPreviewDate <= @today) or c.ActiveTerminationLetterDueDate is not null or c.ActivePostponedUntilDate is not null) 
                                  and   c.CreditNr = @creditNr";

                return Query<DateTime?>(context, query, additionalParameters: new Dictionary<string, object> { { "creditNr", creditNr } }).FirstOrDefault();
            }
        }

        public class GetTerminationLetterCandidatesPageResult
        {
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public List<PageItem> Page { get; set; }
            public class PageItem
            {
                public string CreditNr { get; internal set; }
                public string CreditUrl { get; internal set; }
                public int NrUnpaidOverdueNotifications { get; internal set; }
                public DateTime? TerminationCandidateDate { get; set; }
                public decimal BalanceUnpaidOverdueNotifications { get; internal set; }
                public decimal InitialUnpaidOverdueNotifications { get; internal set; }
                public decimal FractionBalanceUnpaidOverdueNotifications { get; internal set; }
                public int NrOfDaysOverdue { get; internal set; }
                public bool HasAttention { get; internal set; }
                public DateTime? AttentionHasRecentOverdueTerminationLetter { get; internal set; }
                public decimal? AttentionNotificationLowBalanceAmount { get; internal set; }
                public decimal? AttentionTotalLowBalanceAmount { get; internal set; }
                public DateTime? AttentionWasPostponedUntilDate { get; internal set; }
                public DateTime? AttentionPromisedToPayDateRecentOrFuture { get; internal set; }
                public bool IsEligableForTerminationLetter { get; internal set; }
                public bool IsEligableForTerminationLetterExpectDate { get; set; }
                public DateTime? ActivePostponedUntilDate { get; internal set; }
                public DateTime? ActiveTerminationLetterDueDate { get; internal set; }
                public DateTime? AttentionSettlementOfferDate { get; internal set; }
                public DateTime? TerminationPreviewDate { get; set; }
            }
        }

        private class TerminationLetterCandidateModel
        {
            public string CreditNr { get; set; }
            public decimal BalanceAmountUnpaidOverdueNotifications { get; set; }
            public decimal InitialAmountUnpaidOverdueNotifications { get; set; }
            public int NrUnpaidOverdueNotifications { get; set; }
            public int NrOfDaysOverdue { get; set; }
            public bool HasAttention { get; set; }
            public decimal? AttentionNotificationLowBalanceAmount { get; set; }
            public decimal? AttentionTotalLowBalanceAmount { get; set; }
            public DateTime? AttentionWasPostponedUntilDate { get; set; }
            public DateTime? ActiveTerminationLetterDueDate { get; set; }
            public DateTime? AttentionHasRecentOverdueTerminationLetter { get; set; }
            public DateTime? AttentionPromisedToPayDateRecentOrFuture { get; set; }
            public DateTime? AttentionSettlementOfferDate { get; set; }
            public bool IsEligableForTerminationLetter { get; set; }
            public bool IsEligableForTerminationLetterExpectDate { get; set; }
            public DateTime? ActivePostponedUntilDate { get; set; }
            /// <summary>
            /// The earliest date that termination letters will actually be sent.
            /// This will be TerminationPreviewDate + terminationGraceDays
            /// </summary>
            public DateTime? TerminationCandidateDate { get; set; }
            /// <summary>
            /// First date when it's legally possible to send a letter.
            /// Actual letters will be sent terminationGraceDays days after this
            /// </summary>
            public DateTime? TerminationPreviewDate { get; set; }
        }

    }
}