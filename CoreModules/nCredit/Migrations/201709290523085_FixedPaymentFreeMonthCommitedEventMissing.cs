namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FixedPaymentFreeMonthCommitedEventMissing : DbMigration
    {
        public override void Up()
        {
            Sql(
@"update	f
set		f.CommitedByEventBusinessEventId = a.CreatedByBusinessEventId
from	CreditFuturePaymentFreeMonth f
join	CreditPaymentFreeMonth a on a.CreditNr = f.CreditNr and year(f.ForMonth) = YEAR(a.DueDate) and month(f.ForMonth) = month(a.DueDate)
where	f.CancelledByBusinessEventId is null
and		f.CommitedByEventBusinessEventId is null");
        }

        public override void Down()
        {
        }
    }
}
