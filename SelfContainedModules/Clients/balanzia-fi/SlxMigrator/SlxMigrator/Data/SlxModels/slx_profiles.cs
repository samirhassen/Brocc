using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_profiles
    {
		public static string GetSavingsKey(string savingsAccountNr, int customerId) => $"{savingsAccountNr}#{customerId}";

		public static Dictionary<string, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, bool isLoan) =>
					isLoan ? CreateForCustomersForLoans(customerIds, connectionFactory) : CreateForCustomersForSavings(customerIds, connectionFactory);
		
		private static Dictionary<string, List<JObject>> CreateForCustomersForSavings(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			//See: https://naktergal.atlassian.net/wiki/spaces/UD/pages/45318161/Change+interest+rate+-+savings for why this is complicated
			var query = @"with AccountExt
as
(
	select	h.SavingsAccountNr,
			h.CreatedByBusinessEventId,
			h.MainCustomerId,
			(select top 1 b.TransactionDate from BusinessEvent b where b.Id = h.CreatedByBusinessEventId) as AccountCreatedDate,
			(select top 1 s.TransactionDate from DatedSavingsAccountString s where s.SavingsAccountNr = h.SavingsAccountNr and s.Name = 'SavingsAccountStatus' and s.Value = 'Closed') as AccountClosedDate
	from	SavingsAccountHeader h	
)
,
RatesPre1
as
(
	select	h.SavingsAccountNr,
			r.ValidFromDate,
			r.InterestRatePercent,			
			h.AccountCreatedDate,
			h.AccountClosedDate,
			h.MainCustomerId,
			r.Id as InterestId,
			isnull(r.AppliesToAccountsSinceBusinessEventId, 0) as AppliesToAccountsSinceBusinessEventId,						
			r.BusinessEventId,
			case when r.ValidFromDate <= h.AccountCreatedDate then 1 else 0 end as IsFromOrBeforeCreationDate
	from	SharedSavingsInterestRate r
	cross join AccountExt h
	where	r.AccountTypeCode = 'StandardAccount'
	and		r.RemovedByBusinessEventId is null
	and		(r.AppliesToAccountsSinceBusinessEventId is null or h.CreatedByBusinessEventId >= r.AppliesToAccountsSinceBusinessEventId)
	and		(h.AccountClosedDate is null or r.ValidFromDate <= h.AccountClosedDate)
),
RatesPre2
as
(
	select	h.*,
			RANK() OVER (PARTITION BY h.SavingsAccountNr, h.IsFromOrBeforeCreationDate ORDER BY h.ValidFromDate desc, h.InterestId desc) AS BeforeRank
	from	RatesPre1 h
),
RatesPre3
as
(
	select	h.*,
			RANK() OVER (PARTITION BY h.SavingsAccountNr ORDER BY h.ValidFromDate asc, h.InterestId asc) as InterestRank
	from	RatesPre2 h
	where	(h.IsFromOrBeforeCreationDate = 0 or h.IsFromOrBeforeCreationDate = 1 and h.BeforeRank = 1)
),
RatesPre4
as
(
	select	h.*,
			(select top 1 n.ValidFromDate from RatesPre3 n where n.SavingsAccountNr = h.SavingsAccountNr and n.InterestRank = (h.InterestRank + 1)) as NextValidFromDate
	from	RatesPre3 h
)
select	h.SavingsAccountNr as savings_id,
		h.MainCustomerId as customer_id,
		h.InterestRatePercent,
		convert(nvarchar, case when h.ValidFromDate < h.AccountCreatedDate then h.AccountCreatedDate else h.ValidFromDate end, 23) as start_datetime,
		convert(nvarchar, case 
			when h.NextValidFromDate is null then h.AccountClosedDate
			when h.AccountClosedDate is null then h.NextValidFromDate
			when h.NextValidFromDate < h.AccountClosedDate then h.NextValidFromDate
			else h.AccountClosedDate
		end, 23) as end_datetime
from	RatesPre4 h
where   h.MainCustomerId in @customerIds";
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Savings))
			{
				var profiles = creditConnection.Query<object>(query, param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return profiles
					.GroupBy(x => GetSavingsKey(
						x["savings_id"].Value<string>(), 
						x["customer_id"].Value<int>()))
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}

		private static Dictionary<string, List<JObject>> CreateForCustomersForLoans(HashSet<int> customerIds, ConnectionFactory connectionFactory)
        {
			throw new Exception("Only exists for savings");
        }
	}
}
