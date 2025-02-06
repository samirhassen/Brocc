using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_invoice_line_items
    {
		public static Dictionary<int, Dictionary<int, List<JObject>>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
			{
				var loans = creditConnection.Query<object>(@"select	t.Id as lineitem_id,
		t.CreditNotificationId as invoice_id,
		n.CreditNr as loan_identifier,
		c.CustomerId as customer_identifier,
		case 
			when t.AccountCode = 'NotNotifiedCapital' then 'amortization'
			when t.AccountCode = 'CapitalDebt' then 'amortization'
			when t.AccountCode = 'InterestDebt' then 'accrued_interest'
			when t.AccountCode = 'ReminderFeeDebt' then 'reminder_fee'
			when t.AccountCode = 'NotificationFeeDebt' then 'invoicing_admin_fee'
			else t.AccountCode
		end as line_item,
		'' as [description],
		(select isnull(sum(case when tt.AccountCode = 'NotNotifiedCapital' then -tt.Amount else tt.Amount end), 0) from AccountTransaction tt where tt.CreditNotificationId = t.CreditNotificationId and tt.AccountCode in('NotNotifiedCapital', 'CapitalDebt') and tt.Id <= t.Id) as principal_amount,
		(select isnull(sum(case when tt.AccountCode = 'NotNotifiedCapital' then -tt.Amount else tt.Amount end), 0) from AccountTransaction tt where tt.CreditNotificationId = t.CreditNotificationId and tt.Id <= t.Id) as total_amount,
		case when t.AccountCode = 'NotNotifiedCapital' then -t.Amount else t.Amount end as paid_amount,
		b.EventType as ntech_bussiness_event_type,
		b.Id as ntech_bussiness_event_id,
		case 
			when t.WriteoffId is not null then 'writeoff'
			when t.IncomingPaymentId is not null then 'payment'
			else 'initial'
		end as ntech_transaction_type
from	AccountTransaction t
join	CreditNotificationHeader n on n.Id = t.CreditNotificationId
join	CreditCustomer c on c.CreditNr = n.CreditNr
join	BusinessEvent b on b.Id = t.BusinessEventId
where	t.CreditNotificationId is not null
and	    c.CustomerId in @customerIds", param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_identifier"].Value<int>())
					.ToDictionary(
						x => x.Key, 
						x => x
							.GroupBy(y => y["invoice_id"].Value<int>())
							.ToDictionary(z => z.Key, z => z.ToList()));
			}
		}
	}
}
