using Dapper;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace SlxMigrator
{
    internal class slx_transactions
    {
		public static Dictionary<int, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, bool isLoan) =>
			isLoan ? CreateForCustomersForLoans(customerIds, connectionFactory) : CreateForCustomersForSavings(customerIds, connectionFactory);        

		private static Dictionary<int, List<JObject>> CreateForCustomersForLoans(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			var query = @"select	t.Id as transaction_id,
		c.CustomerId as customer_id,
		h.CreditNr as loan_id,
		t.TransactionDate as registration_datetime,
		t.BookKeepingDate as reference_datetime,
		e.EventType as transaction_name,
		case when t.Amount < 0 then 'credit' else 'debit' end as transaction_type,
		t.Amount as amount
from	AccountTransaction t
join	CreditHeader h on t.CreditNr = h.CreditNr
join	CreditCustomer c on c.CreditNr = h.CreditNr
join	BusinessEvent e on e.Id = t.BusinessEventId
where	t.AccountCode = 'CapitalDebt'
and	    c.CustomerId in @customerIds
";
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
			{
				var loans = creditConnection.Query<object>(query, param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}

		private static Dictionary<int, List<JObject>> CreateForCustomersForSavings(HashSet<int> customerIds, ConnectionFactory connectionFactory)
        {
			var query =
@"select	t.Id as transaction_id,
		h.SavingsAccountNr as savings_id,
		h.MainCustomerId as customer_id,
		t.TransactionDate as registration_datetime,
		t.BookKeepingDate as reference_datetime,
		case when t.BusinessEventRoleCode is null 
			then b.EventType 
			else (b.EventType + '_' + t.BusinessEventRoleCode)
		end as transaction_name,
		case when t.Amount < 0 then 'credit' else 'debit' end as transaction_type,
		t.Amount as amount,
		t.AccountCode
from	SavingsAccountHeader h
join	LedgerAccountTransaction t on h.SavingsAccountNr = t.SavingsAccountNr
join	BusinessEvent b on t.BusinessEventId = b.Id
where	t.AccountCode = 'Capital'
and	    h.MainCustomerId in @customerIds";
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Savings))
			{
				var loans = creditConnection.Query<object>(query, param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}
	}
}
