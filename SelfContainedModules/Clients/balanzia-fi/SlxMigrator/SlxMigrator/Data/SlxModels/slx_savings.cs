using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_savings
    {
		public static Dictionary<int, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			using (var savingsConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Savings))
			{
				var query =
@"select	a.SavingsAccountNr as savings_id,
		a.SavingsAccountNr as saving_account_number,
		a.MainCustomerId as customer_id,
		0 as legal_representative_id,
		'' as [name],
		(select convert(varchar, MAX(s.TransactionDate), 23) from DatedSavingsAccountString s where s.SavingsAccountNr = a.SavingsAccountNr and s.[Name] = 'SavingsAccountStatus' and s.[Value] = 'Active') as activation_datetime,
		(select convert(varchar, MAX(s.TransactionDate), 23) from DatedSavingsAccountString s where s.SavingsAccountNr = a.SavingsAccountNr and s.[Name] = 'SavingsAccountStatus' and s.[Value] = 'Closed') as deactivation_datetime,
		convert(varchar, b.TransactionDate, 23) as creation_datetime,
		(select top 1 s.[Value] from DatedSavingsAccountString s where s.SavingsAccountNr = a.SavingsAccountNr and s.[Name] = 'SignedInitialAgreementArchiveKey') as agreement_file_id,
		case (select top 1 q.[Value] from SavingsAccountKycQuestion q where q.SavingsAccountNr = a.SavingsAccountNr and q.[Name] = 'initialdepositrangeestimate' order by Id desc)
			when '0_100' then 50
			when '100_1000' then 500
			when '1000_10000' then 5000
			when '10000_50000' then 40000
			when '50000_80000' then 65000
			when '80000_max' then 90000
			else ''
		end as initial_deposit_amount
from	SavingsAccountHeader a
join	BusinessEvent b on b.Id = a.CreatedByBusinessEventId
where	a.MainCustomerId in @customerIds
order by a.CreatedByBusinessEventId asc";
				var loans = savingsConnection.Query<object>(query, param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}
	}
}
