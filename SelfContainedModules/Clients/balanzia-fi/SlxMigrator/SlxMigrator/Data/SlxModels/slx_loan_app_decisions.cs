using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_loan_app_decisions
    {
		public static string GetKey(string applicationNr, int customerId) => $"{applicationNr}#{customerId}";

		public static Dictionary<string, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			using (var preCreditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.PreCredit))
			{
				var query =
@"with CreditApplicationCustomer
as
(
	select	i.ApplicationNr,
			1 as ApplicantNr,
			cast(i.[Value] as int) as CustomerId,
			i.GroupName as ApplicantGroupName
	from	CreditApplicationItem i where i.GroupName = 'Applicant1' and i.[Name] = 'customerId'
	union all
	select	i.ApplicationNr,
			2 as ApplicantNr,
			cast(i.[Value] as int),
			i.GroupName as ApplicantGroupName
	from	CreditApplicationItem i where i.GroupName = 'Applicant2' and i.[Name] = 'customerId'
),
SlxApplicationPre
as
(
	select	d.Id,
			c.ApplicationNr as fk_application,
			c.CustomerId as fk_applicant,
			c.ApplicationNr as fk_detail,
			case c.ApplicantNr 
				when 1 then CAST(1 as bit)
				else CAST(0 as bit)
			end as is_main_applicant,
			'application' as [source],
			IIF(d.Discriminator = 'AcceptedCreditDecision', 'accept', 'reject') + '_'
			+ IIF(d.WasAutomated = 1, 'auto', 'manual') as [type],
			'' as decision
	from	CreditApplicationCustomer c
	join	CreditApplicationHeader h on h.ApplicationNr = c.ApplicationNr
	join CreditDecision d on d.Id = h.CurrentCreditDecisionId
	where	h.ArchivedDate is null
)
select	p.*
from	SlxApplicationPre p
where	p.fk_applicant in @customerIds";

				var applications = preCreditConnection.Query<object>(query, param: new { customerIds }).Select(JObject.FromObject).ToList();

				return applications
					.GroupBy(x => GetKey(
						x["fk_application"].Value<string>(),
						x["fk_applicant"].Value<int>()))
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}
	}
}
