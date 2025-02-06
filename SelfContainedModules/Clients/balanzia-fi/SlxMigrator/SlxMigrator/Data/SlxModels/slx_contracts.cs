using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_contracts
    {
		private static List<JObject> CreateInitialContractsForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, CrossRunCacheDb crossRunCacheDb)
		{
			var initialContractsQuery =
@"with CreditHeaderExtended
		as
		(
			select	h.*,
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
		),
		PreInitialContract
		as
		(
			select	'unsecured_variable_rate_annuity_loan' as contract_type,
					0 as loan_product_number,
					h.CreditNr as loan_id,
					c.CustomerId as customer_id,
					(select cc.CustomerId from CreditCustomer cc where cc.CreditNr = c.CreditNr and cc.CustomerId <> c.CustomerId) as co_customer_id,
					(select sum(t.Amount) from AccountTransaction t join BusinessEvent b on b.Id = t.BusinessEventId where t.CreditNr = h.CreditNr and t.AccountCode = 'CapitalDebt' and b.EventType in ('NewCredit', 'CapitalizedInitialFee')) as size,
					h.InitialInterestRate as interest_rate,
					CEILING(Log10((0-h.InitialAnnuityAmount)
						/ (-h.InitialAnnuityAmount + (h.InitialInterestRate/100/12) * h.RealInitialCapitalDebt))
						/ Log10(1 + (h.InitialInterestRate/100/12))) * 30 as term_in_days,
					'monthly' as repayment_periodicity,
					CEILING(Log10((0-h.InitialAnnuityAmount)
								/ (-h.InitialAnnuityAmount + (h.InitialInterestRate/100/12) * h.RealInitialCapitalDebt))
								/ Log10(1 + (h.InitialInterestRate/100/12))) as repayment_periods,
					'annuity' as repayment_method,
					h.InitialAnnuityAmount as period_cost,
					'fixed' as interest_type,
					convert(varchar, h.StartDate, 23) as [start_date],
					'' as grade,
					0 as slx_probability_of_default
			from	CreditHeaderExtended h
			join	CreditCustomer c on c.CreditNr = h.CreditNr
		),
		InitialContract
		as
		(
			select	p.contract_type,
					p.loan_product_number,
					p.loan_id,
					p.customer_id,
					p.co_customer_id,
					p.size,
					p.interest_rate,
					0 as effective_interest_rate,
					p.term_in_days,
					p.repayment_periodicity,
					p.repayment_periods,
					p.period_cost,
					p.interest_type,
					p.[start_date],
					DATEADD(d, p.term_in_days, p.start_date) as end_date,
					p.grade,
					p.slx_probability_of_default
			from	PreInitialContract p
		)
		select	c.*
		from	InitialContract c
		where   c.customer_id in @customerIds";
			
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
			{
				var initialContracts = creditConnection.Query<object>(initialContractsQuery, param: new { customerIds }).Select(JObject.FromObject).ToList();
				var creditNrs = initialContracts.Select(x => x.GetStringPropertyValue("loan_id", false)).ToHashSet();
				var effRateByCreditNr = crossRunCacheDb.GetInitialEffectiveInterestRatesForCredits(creditNrs);
				foreach(var contract in initialContracts)
                {
					contract.AddOrReplaceJsonProperty("effective_interest_rate", new JValue(effRateByCreditNr[contract.GetStringPropertyValue("loan_id", false)]), true);
                }

				return initialContracts;
			}
		}

		public static Dictionary<string, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, CrossRunCacheDb crossRunCacheDb)
        {
			var initialContracts = CreateInitialContractsForCustomers(customerIds, connectionFactory, crossRunCacheDb);

			var allContracts = initialContracts; //.Concat(additionalLoanContracts) ... and so on

			return allContracts
				.GroupBy(x => GetKey(x["customer_id"].Value<int>(), x["loan_id"].Value<string>()))
				.ToDictionary(x => x.Key, x => x.ToList());
		}

		public static string GetKey(int customerId, string creditNr) => $"{customerId}#{creditNr}";
	}
}
