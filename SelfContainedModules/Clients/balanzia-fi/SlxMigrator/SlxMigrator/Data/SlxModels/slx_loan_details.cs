using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_loan_details
    {
        public static Dictionary<string, List<JObject>> CreateKeyedByLoanId(HashSet<int> customerIds, ConnectionFactory connectionFactory)
        {
            //NOTE: These are not per customer unlike most other things.
            var query = string.Format(@"declare @interestDate Date = cast(getdate() as date);
WITH 
{0},
AccruedInterestCredits
as
(
	select	h.CreditNr,
			(select top 1 cast(v.Value as date)  from DatedCreditString v where v.CreditNr = h.CreditNr and v.Name = 'NextInterestFromDate' and v.TransactionDate <= @interestDate order by v.[Timestamp] desc) as NextInterestFromDate,
			(select top 1 v.Value  from DatedCreditString v where v.CreditNr = h.CreditNr and v.Name = 'CreditStatus' and v.TransactionDate <= @interestDate order by v.[Timestamp] desc) as CreditStatus
	from	CreditHeader h
),
AccruedInterestBasis
as
(
	SELECT	d.TheDate,
			c.CreditNr,
			c.CreditStatus as CurrentCreditStatus,
			c.NextInterestFromDate as CurrentCreditNextInterestFromDate,
			(select SUM(t.Amount) from AccountTransaction t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr and t.TransactionDate <= d.TheDate) as CapitalDebtAmount,
			((select top 1 v.Value from DatedCreditValue v where v.CreditNr = c.CreditNr and v.Name = 'MarginInterestRate' and v.TransactionDate <= d.TheDate order by v.[Timestamp] desc)
			+
			(select top 1 v.Value from DatedCreditValue v where v.CreditNr = c.CreditNr and v.Name = 'ReferenceInterestRate' and v.TransactionDate <= d.TheDate order by v.[Timestamp] desc)) as InterestRate
	FROM	CalendarDate d
	cross join	AccruedInterestCredits c
),
AccruedInterestDailyInterestAmount
as
(
	select	b.*,
			b.CapitalDebtAmount * b.InterestRate / 365.25 / 100 as DayInterestAmount
	from	AccruedInterestBasis b
	where	b.TheDate >= b.CurrentCreditNextInterestFromDate
	and		b.TheDate <= @interestDate
	and		b.CurrentCreditStatus = 'Normal'
),
AccruedInterestRateByCreditNr
as
(
	select	d.CreditNr,
			SUM(d.DayInterestAmount) as TotalAmount
	from	AccruedInterestDailyInterestAmount d
	group by d.CreditNr
),
CreditHeaderExtended
as
(
	select	h.*,
			(select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'AnnuityAmount' and d.CreditNr = h.CreditNr order by d.Id desc) as CurrentAnnuityAmount,
			(select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'ReferenceInterestRate' and d.CreditNr = h.CreditNr order by d.Id desc) as CurrentReferenceInterestRate,
			(select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'MarginInterestRate' and d.CreditNr = h.CreditNr order by d.Id desc) as CurrentMarginInterestRate,
			(select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'RequestedMarginInterestRate' and d.CreditNr = h.CreditNr order by d.Id desc) as CurrentRequestedMarginInterestRate,			
			(select sum(a.Amount) from AccountTransaction a where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr) as CurrentCapitalDebt,			
			((select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'MarginInterestRate' and d.BusinessEventId = h.CreatedByBusinessEventId)
			+ (select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'ReferenceInterestRate' and d.BusinessEventId = h.CreatedByBusinessEventId))  as InitialInterestRate,
			(select top 1 d.[Value] from DatedCreditValue d where d.[Name] = 'AnnuityAmount' and d.BusinessEventId = h.CreatedByBusinessEventId) as InitialAnnuityAmount,
			(select sum(a.Amount) from AccountTransaction a join BusinessEvent b on b.Id = a.BusinessEventId  where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr and b.EventType in('NewCredit', 'CapitalizedInitialFee')) as RealInitialCapitalDebt,
			(
				(select sum(a.Amount) from AccountTransaction a join BusinessEvent b on b.Id = a.BusinessEventId  where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr and b.EventType in('NewCredit', 'CapitalizedInitialFee', 'NewAdditionalLoan')) 
				+
				(select isnull(sum(a.Amount), 0) from AccountTransaction a where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr and a.CreditPaymentFreeMonthId is not null)
			) as AllPositiveInitialCapitalDebt,
			(select sum(a.Amount) from AccountTransaction a join BusinessEvent b on b.Id = a.BusinessEventId  where a.AccountCode = 'CapitalDebt' and a.CreditNr = h.CreditNr and b.EventType in('CapitalizedInitialFee')) as InitialFeeAmount,
			(select top 1 s.TransactionDate from DatedCreditString s where s.[Name] = 'CreditStatus' and s.CreditNr = h.CreditNr order by s.BusinessEventId desc) as CurrentStatusDate
	from	CreditHeader h
)
select	h.CreditNr as detail_id,
		convert(varchar, getdate(), 23) as reference_datetime,
		h.CreditNr as loan_id,
		case 
			when h.[Status] = 'SentToDebtCollection' then 'collection'
			when h.[Status] = 'Settled' then 'ended'
			when h.[Status] = 'Normal' and exists(select 1 from CreditNotificationHeader h where h.DueDate < cast(getdate() as date) and h.ClosedTransactionDate is null) then 'overdue'
			else 'current'
		end as loan_performance_status,
		(select round(isnull(t.TotalAmount, 0), 2) from AccruedInterestRateByCreditNr t where t.CreditNr = h.CreditNr) as accrued_interest,
		case 
			when h.[Status] = 'Normal' then (select max(i.end_date) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'paid')
			else h.CurrentStatusDate
		end as interest_paid_to_date,
		case 
			when h.[Status] = 'Normal' then (select max(i.end_date) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'paid')
			else h.CurrentStatusDate
		end as principal_paid_to_date,
		(select datediff(d, min(i.due_date), cast(getdate() as date)) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'outstanding') as payment_days_overdue,
		CEILING(Log10((0-h.CurrentAnnuityAmount)
					/ (-h.CurrentAnnuityAmount + ((h.CurrentMarginInterestRate + h.CurrentReferenceInterestRate) /100/12) * h.CurrentCapitalDebt))
					/ Log10(1 + (h.InitialInterestRate/100/12))) * 30 as remaining_term_in_days,
		h.CurrentCapitalDebt as nominal_value,
		(select isnull(-sum(t.Amount), 0) from AccountTransaction t where t.CreditNr = h.CreditNr and t.AccountCode = 'CapitalDebt' and t.WriteoffId is not null) as write_off,
		(select count(*) from CreditNotificationHeader n where n.CreditNr = h.CreditNr) as total_invoices_count,
		(select count(*) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'outstanding') as outstanding_invoices_count,
		(select count(*) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'paid' and i.end_date > i.due_date) as late_invoices_count,
		(select count(*) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'paid') as paid_invoices_count,
		(select count(*) from SlxInvoice i where i.invoice_id in (select n.Id from CreditNotificationHeader n where n.CreditNr = h.CreditNr) and i.status = 'cancelled') as cancelled_invoices_count,
		0 as credited_invoices_count
from	CreditHeaderExtended h
where	exists(select 1 from CreditCustomer c where c.CreditNr = h.CreditNr and c.CustomerId in @customerIds)
", slx_invoices.InvoiceExpression);
            using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
            {
                var loanDetails = creditConnection.Query<object>(query, param: new { customerIds }).Select(JObject.FromObject).ToList();
                return loanDetails
                    .GroupBy(x => x["loan_id"].Value<string>())
                    .ToDictionary(x => x.Key, x => x.ToList());               
            }
        }
    }
}
