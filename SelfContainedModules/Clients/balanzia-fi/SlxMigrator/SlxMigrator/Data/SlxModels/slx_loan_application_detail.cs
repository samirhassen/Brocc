using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_loan_application_detail
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
select	c.ApplicationNr as loan_application_detail_id,
		convert(varchar, getdate(), 23) as reference_datetime,
		c.ApplicationNr as loan_application_id,
		c.CustomerId as customer_id,
		0 as uc_probability_of_default,
		case (select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'marriage' and i.GroupName = c.ApplicantGroupName)
			when 'marriage_ogift' then 'single'
			when 'marriage_sambo' then 'living_together'
			when 'marriage_gift' then 'married'
			else ''
		end as marital_status,
		case (select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employment' and i.GroupName = c.ApplicantGroupName)
			when 'employment_arbetslos' then 'unemployed'
			when 'employment_fastanstalld' then 'full_time'
			when 'employment_foretagare' then 'self_employed'
			when 'employment_pensionar' then 'pensioner'
			when 'employment_sjukpensionar' then 'early_pensioner'
			when 'employment_studerande' then 'student'
			when 'employment_visstidsanstalld' then 'finite_time'
			else ''
		end as occupation,
		cast((select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'nrOfChildren' and i.GroupName = c.ApplicantGroupName) as int) as number_of_kids,
		cast((select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'incomePerMonthAmount' and i.GroupName = c.ApplicantGroupName) as int) as work_income,
		cast((select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'housingCostPerMonthAmount' and i.GroupName = c.ApplicantGroupName) as int) as housing_cost,
		cast((select top 1 cast(LEFT(REPLACE(i.[Value], '-', ''), 4) as int) from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employedSinceMonth' and i.GroupName = c.ApplicantGroupName) as int) as occupation_from_year,
		cast((select top 1 cast(RIGHT(REPLACE(i.[Value], '-', ''), 2) as int) from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employedSinceMonth' and i.GroupName = c.ApplicantGroupName) as int) as occupation_from_month,
		(select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employer' and i.GroupName = c.ApplicantGroupName) as employer,
		(select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employerPhone' and i.GroupName = c.ApplicantGroupName) as telephone_to_employer,
		cast((select top 1 cast(LEFT(REPLACE(i.[Value], '-', ''), 4) as int) from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employedUntilDate' and i.GroupName = c.ApplicantGroupName) as int) as occupation_to_year,
		cast((select top 1 cast(RIGHT(LEFT(REPLACE(i.[Value], '-', ''), 6), 2) as int) from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'employedUntilDate' and i.GroupName = c.ApplicantGroupName) as int) as occupation_to_month,
		'' as identifier_to_uc,
		null as weighted_left_to_live_on,
		(select	isnull(sum(cast(i.[Value] as int)), 0)
		from	CreditApplicationItem i
		where	i.[Name] in ('creditCardCostPerMonthAmount', 'mortgageLoanCostPerMonthAmount', 'studentLoanCostPerMonthAmount', 'carOrBoatLoanCostPerMonthAmount', 'otherLoanCostPerMonthAmount', 'housingCostPerMonthAmount')
		and		i.ApplicationNr = h.ApplicationNr) as monthly_liabilities
from	CreditApplicationCustomer c
join	CreditApplicationHeader h on h.ApplicationNr = c.ApplicationNr
left outer join CreditDecision d on d.Id = h.CurrentCreditDecisionId
where	h.ArchivedDate is null
)
select	p.*
from	SlxApplicationPre p
where	p.customer_id in @customerIds";

				var applications = preCreditConnection.Query<object>(query, param: new { customerIds }).Select(JObject.FromObject).ToList();

				return applications
					.GroupBy(x => GetKey(
						x["loan_application_id"].Value<string>(),
                        x["customer_id"].Value<int>()))
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}
	}
}
