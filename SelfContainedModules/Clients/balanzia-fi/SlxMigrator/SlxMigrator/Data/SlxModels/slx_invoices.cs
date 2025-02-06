using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_invoices
    {
		public static Dictionary<int, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory)
		{
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
			{
				var loans = creditConnection.Query<object>(
		String.Format(@"with {0} select * from SlxInvoice i where i.customer_id in @customerIds", InvoiceExpression), param: new { customerIds }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}

		public const string InvoiceExpression = @"DebtCollectionWriteOffNotificationId
as
(
	select	t.CreditNotificationId
	from	AccountTransaction t
	join	BusinessEvent b on b.Id = t.BusinessEventId
	where	t.CreditNotificationId is not null
	and		b.EventType = 'CreditDebtCollectionExport'
	and		t.WriteoffId is not null
),
SlxNotificationPre
as
(
select	n.Id as invoice_id,
		c.CustomerId as customer_id,
		cast(convert(nvarchar, n.DueDate, 112) as int) as invoice_number,		
		'regular_mail' as invoice_type,
		convert(nvarchar, n.NotificationDate, 23) as [start_date],
		convert(nvarchar, n.ClosedTransactionDate, 23) as end_date,
		convert(nvarchar, n.NotificationDate, 23) as bill_date,
		convert(nvarchar, n.DueDate, 23) as due_date,
		(select isnull(sum(case when t.AccountCode = 'NotNotifiedCapital' then -t.Amount else t.Amount end), 0) from AccountTransaction t join BusinessEvent b on b.Id = t.BusinessEventId where t.CreditNotificationId = n.Id and b.EventType in('NewNotification', 'NewReminder')) as total_amount,
		-(select isnull(sum(t.Amount), 0) from AccountTransaction t where t.CreditNotificationId = n.Id and t.IncomingPaymentId is not null and t.WriteoffId is null) as paid_total_amount,
		-(select isnull(sum(case when t.AccountCode = 'NotNotifiedCapital' then -t.Amount else t.Amount end), 0) from AccountTransaction t where t.CreditNotificationId = n.Id and t.WriteoffId is not null) as written_off_total_amount,
		-(select isnull(sum(t.Amount), 0) from AccountTransaction t where t.CreditNotificationId = n.Id and t.IncomingPaymentId is not null and t.WriteoffId is null and t.AccountCode = 'CapitalDebt') as settled_credit_amount,
		n.CreditNr,
		n.ClosedTransactionDate,
		n.Id as NotificationId
from	CreditNotificationHeader n
join	CreditHeader h on h.CreditNr = n.CreditNr
join	CreditCustomer c on c.CreditNr = h.CreditNr
),
SlxInvoice
as
(
select	p.invoice_id,
		p.customer_id,
		p.invoice_number,
		p.[status],
		p.invoice_type,
		p.[start_date],
		p.end_date,
		p.bill_date,
		p.due_date,
		p.total_amount,
		p.paid_total_amount,		
		p.settled_credit_amount,
		(case when p.[status] = 'paid' then p.ClosedTransactionDate else null end) as paid_at,
		cast(0 as bit) as insurance,
		p.written_off_total_amount
from	(	select p1.*,
			case 
				when p1.ClosedTransactionDate is not null and exists(select '1' from DebtCollectionWriteOffNotificationId i where i.CreditNotificationId = p1.NotificationId) then 'cancelled'
				when p1.ClosedTransactionDate is not null and p1.written_off_total_amount > 0 and p1.written_off_total_amount = p1.total_amount then 'cancelled'
				when p1.ClosedTransactionDate is not null then 'paid'
				else 'outstanding'
			end as [status]
			from SlxNotificationPre p1) p
)";
	}
}
