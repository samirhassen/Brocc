namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FixUnplacedPaymentTransactionBug : DbMigration
    {
        public override void Up()
        {
            //We used to always book payments to unplaced when they came in and then place them and count the balance on unplaced down
            //Later it was changed so that payments that were placed right away were never booked to unplaced but the logic that booked it down remained in place
            //It has now been fixed in code but we need to remove the dangling minus-transactions. They should have had no effect on the system as they
            //are not part of the bookkeeping matrix.
            Sql(
@"delete from AccountTransaction where Id in
(
	select	a.Id
	from	AccountTransaction a
	where	a.AccountCode = 'UnplacedPayment'
	and		a.IncomingPaymentId is not null
	and		a.CreditNr is not null
	and		a.OutgoingBookkeepingFileHeaderId is null
	and		not exists(select 1 from AccountTransaction b where b.AccountCode = a.AccountCode and b.IncomingPaymentId = a.IncomingPaymentId and b.CreditNr is null)
)");
        }

        public override void Down()
        {
        }
    }
}
