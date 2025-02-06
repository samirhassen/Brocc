using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;
namespace nCredit.WebserviceMethods
{
	public class DashboardDailyAggregatesMethod : TypedWebserviceMethod<DashboardDailyAggregatesMethod.Request, DashboardDailyAggregatesMethod.Response>
	{
		public override string Path => "Dashboard/Aggregate-Data";

		protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
		{
			using (var context = new CreditContext())
			{
				return context.Database.SqlQuery<Response>(@"with CreditInitialPayment
as
(
	select	t.TransactionDate as [Date], 
			t.CreditNr, 
			sum(t.Amount) as Amount
	from	AccountTransaction t
	join	BusinessEvent e on e.Id = t.BusinessEventId
	where	t.AccountCode = 'CapitalDebt'
	and		e.EventType = 'NewCredit'
	group by t.TransactionDate, t.CreditNr
), DailyAmount
as
(
	select	c.[Date], SUM(c.Amount) as DailyAmount
	from	CreditInitialPayment c
	group by c.[Date]
),
CurrentDatedCreditValuePre
as
(
	select	d.*,			
			(RANK() OVER (PARTITION BY d.CreditNr, d.Name ORDER BY d.TransactionDate DESC, d.Id Desc)) as RankNr
	from	DatedCreditValue d	
),
CurrentDatedCreditValue
as
(
	select	p.CreditNr, p.Name, p.TransactionDate, p.Value
	from	CurrentDatedCreditValuePre p
	where	p.RankNr = 1
),
Credit1
as
(
	select	h.CreditNr,
			(select SUM(t.Amount) from AccountTransaction t where t.CreditNr = h.CreditNr and t.AccountCode = 'CapitalDebt') as CapitalBalance,
			(select isnull(max(d.Value), 0) from CurrentDatedCreditValue d where d.CreditNr = h.CreditNr and d.Name = 'ReferenceInterestRate') as ReferenceInterestRate,
			(select isnull(max(d.Value), 0) from CurrentDatedCreditValue d where d.CreditNr = h.CreditNr and d.Name = 'MarginInterestRate') as MarginInterestRate
	from	CreditHeader h
	where	h.[Status] = 'Normal'
)
select	(select COUNT(*) from Credit1) as ActiveLoanCount,
		(select SUM(c.CapitalBalance) from Credit1 c) as CapitalBalance,
		(select sum(c.CapitalBalance * (c.MarginInterestRate + c.ReferenceInterestRate)) / SUM(c.CapitalBalance) from Credit1 c) as AvgActiveLoanInterestRate,
		isnull((select top 1 d.DailyAmount from	DailyAmount d order by d.DailyAmount desc, d.[Date] asc), 0) as MaxDailyPaidAmount,
		(select top 1 d.[Date] from	DailyAmount d order by d.DailyAmount desc, d.[Date] asc) as MaxDailyPaidDate").First();
			}
		}

		public class Request
		{

		}

		public class Response
		{
			public decimal CapitalBalance { get; set; }
			public int ActiveLoanCount { get; set; }
			public decimal AvgActiveLoanInterestRate { get; set; }
			public decimal MaxDailyPaidAmount { get; set; }
			public DateTime? MaxDailyPaidDate { get; set; }
		}
	}
}