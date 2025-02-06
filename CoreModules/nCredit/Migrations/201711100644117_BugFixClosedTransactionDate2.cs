namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class BugFixClosedTransactionDate2 : DbMigration
    {
        public override void Up()
        {
            //The last fix was not very smart as the date has significance in some cases. Setting it to the proper date instead.
            Sql(
@"with MaxTd
as
(
	select	t.CreditNotificationId, max(t.TransactionDate) as LastDate
	from	AccountTransaction t
	where	t.CreditNotificationId is not null
	group by t.CreditNotificationId
)
update h set h.ClosedTransactionDate = t.LastDate
from	CreditNotificationHeader h
join	MaxTd t on t.CreditNotificationId = h.Id
where	h.ClosedTransactionDate is not null
and		h.ClosedTransactionDate <> t.LastDate");
        }

        public override void Down()
        {
        }
    }
}
