using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
	internal class slx_loans
	{
		public static Dictionary<int, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
			{
				var loans = creditConnection.Query<object>(
@"with CreditHeaderExtended
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
select	h.CreditNr as loan_id,
		0 as loan_product_number,
		c.CustomerId as customer_id,
		'Annuity' as loan_type,
		'FI' as loan_market,
		h.AllPositiveInitialCapitalDebt as loan_size,		
		CEILING(Log10((0-h.InitialAnnuityAmount)
			/ (-h.InitialAnnuityAmount + (h.InitialInterestRate/100/12) * h.RealInitialCapitalDebt))
			/ Log10(1 + (h.InitialInterestRate/100/12))) as loan_term,
		CEILING(Log10((0-h.InitialAnnuityAmount)
			/ (-h.InitialAnnuityAmount + (h.InitialInterestRate/100/12) * h.RealInitialCapitalDebt))
			/ Log10(1 + (h.InitialInterestRate/100/12))) * 30 as loan_term_in_days,		
		cast(0 as decimal(18,2)) as slx_probability_of_default,
		h.InitialInterestRate as loan_interest_rate,
		isnull(h.CurrentRequestedMarginInterestRate, h.CurrentMarginInterestRate) as loan_interest_rate_margin,
		case when h.[Status] = 'Normal' then 'active' else 'ended' end as loan_status,
		convert(varchar, h.StartDate, 23) as loan_issuance_date,
		case when h.[Status] <> 'Normal' then convert(varchar, h.CurrentStatusDate, 23) else null end as deactivation_datetime,
		'' as grade,
		cast(h.InitialFeeAmount as int) as loan_application_fee,
		'' as prepayment_reason
from	CreditHeaderExtended h
join	CreditCustomer c on c.CreditNr = h.CreditNr
where	c.CustomerId in @customerIds
order by h.StartDate asc
", param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}
	}
}
