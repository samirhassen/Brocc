using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nDataWarehouse.DbModel
{
    public class DashboardDataRepository : IDisposable
    {
        public class AggregateModel
        {
            public decimal CapitalBalance { get; set; }
            public int ActiveLoanCount { get; set; }
            public decimal AvgActiveLoanInterestRate { get; set; }
            public decimal MaxDailyPaidAmount { get; set; }
            public DateTime? MaxDailyPaidDate { get; set; }
        }

        public class DailyBalanceModel
        {
            public decimal BalanceAmount { get; set; }
            public DateTime TheDate { get; set; }
        }

        public class ApprovedApplicationAmountModel
        {
            public decimal ApprovedAmount { get; set; }
        }

        private Lazy<SqlConnection> connection;

        public void Dispose()
        {
            if (connection.IsValueCreated)
            {
                connection.Value.Dispose();
            }
        }

        public DashboardDataRepository()
        {
            connection = new Lazy<SqlConnection>(() =>
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
                var c = new SqlConnection(connectionString);
                c.Open();
                return c;
            });
        }

        public List<FetchItem> FetchMonthlyPaidOutAmounts(DateTime startDate)
        {
            startDate = new DateTime(startDate.Year, startDate.Month, 1);
            var endDate = startDate.AddMonths(12).AddDays(-1);
            var result = connection.Value.Query<FetchItem>(
                @" select year(a.TransactionDate) as Year, month(a.TransactionDate) as Month, sum(a.Amount) as Amount
                    from Fact_CreditCapitalBalanceEvent a 
                    where a.EventType in ('NewAdditionalLoan', 'NewCredit', 'CapitalizedInitialFee') and a.TransactionDate >= @startDate and a.TransactionDate <= @endDate 
                    group by year(a.TransactionDate), month(a.TransactionDate) 
                    order by year(a.TransactionDate), month(a.TransactionDate)", new { startDate = startDate, endDate = endDate }).ToList();
            var d = startDate;
            var actualResult = new List<FetchItem>();
            while (d <= endDate)
            {
                var i = result.SingleOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                if (i != null)
                    actualResult.Add(i);
                else
                    actualResult.Add(new FetchItem { Year = d.Year, Month = d.Month, Amount = 0 });

                d = d.AddMonths(1);
            }
            return actualResult;
        }

        public class FetchItem
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int Amount { get; set; }

        }
        public List<DailyBalanceModel> FetchDailyBalances()
        {
            return connection.Value.Query<DailyBalanceModel>(
                @"declare @dt datetime, @dtEnd datetime
                        select @dtEnd = max(e.TransactionDate) from	Fact_CreditCapitalBalanceEvent e
                        set @dt = dateadd(day, -365, @dtEnd)
                        ;with Dates
                        as
                        (
	                        select dateadd(day, 3*number, @dt) as TheDate
	                        from 
		                        (select distinct number from master.dbo.spt_values
		                         where name is null
		                        ) n
	                        where dateadd(day, 3*number, @dt) < @dtEnd
	                        union 
	                        select @dtEnd
                        )
                        select	d.TheDate,
		                        (select isnull(sum(b.Amount), 0) from Fact_CreditCapitalBalanceEvent b where b.TransactionDate <= d.TheDate) as BalanceAmount
                        from	Dates d
                        order by d.TheDate asc").ToList();
        }

        public AggregateModel FetchAggregates()
        {
            return connection.Value.Query<AggregateModel>(
                @"with CreditInitialPayment
                    as
                    (
	                    select	f.TransactionDate as [Date], f.CreditNr, sum(f.Amount) as Amount
	                    from	Fact_CreditCapitalBalanceEvent f
	                    where	f.EventType in('NewCredit')
	                    group by f.TransactionDate, f.CreditNr
                    ), DailyAmount
                    as
                    (
	                    select	c.[Date], SUM(c.Amount) as DailyAmount
	                    from	CreditInitialPayment c
	                    group by c.[Date]
                    ),
                    RankedCreditSnapshot
                    as
                    (
	                    select	a.*,
			                    (RANK() OVER (PARTITION BY a.CreditNr ORDER BY a.[Date] DESC)) as RankNr
	                    from	Fact_CreditSnapshot a
                    ),
                    LatestCreditSnapshot
                    as
                    (
	                    select	*
	                    from	RankedCreditSnapshot r
	                    where	r.RankNr = 1
                    )
                    select	isnull((select SUM(CapitalBalance) from LatestCreditSnapshot where [Status] = 'Normal'), 0) as CapitalBalance,
		                    isnull((select count(*) from LatestCreditSnapshot where [Status] = 'Normal'), 0) as ActiveLoanCount,
		                    isnull((select sum(s.CapitalBalance * s.TotalInterestRate) / SUM(s.CapitalBalance) from LatestCreditSnapshot s where s.[Status] = 'Normal'), 0) as AvgActiveLoanInterestRate,
		                    isnull((select top 1 d.DailyAmount from	DailyAmount d order by d.DailyAmount desc, d.[Date] asc), 0) as MaxDailyPaidAmount,
		                    (select top 1 d.[Date] from	DailyAmount d order by d.DailyAmount desc, d.[Date] asc) as MaxDailyPaidDate").Single();
        }

        public ApprovedApplicationAmountModel FetchApprovedApplicationAmount(DateTime fromApprovalDate)
        {
            return connection.Value.Query<ApprovedApplicationAmountModel>(
                @"with RankedCreditApplicationSnapshot
                        as
                        (
	                        select	a.*,
			                        (RANK() OVER (PARTITION BY a.ApplicationNr ORDER BY a.[Date] DESC)) as RankNr
	                        from	Fact_CreditApplicationSnapshot a
                        )
                        select	isnull(sum(a.OfferedAmount), 0) as ApprovedAmount		
                        from	RankedCreditApplicationSnapshot a
                        where	a.RankNr = 1
                        and		a.PartiallyApprovedDate = @fromApprovalDate", new { fromApprovalDate = fromApprovalDate.Date }).Single();
        }
    }
}