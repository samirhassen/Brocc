namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class BugFixClosedTransactionDate : DbMigration
    {
        public override void Up()
        {
            Sql(
@"update CreditNotificationHeader set ClosedTransactionDate = GETDATE() where Id in
(
	select	n.Id
	from	CreditNotificationHeader n
	join	CreditHeader c on c.CreditNr = n.CreditNr and c.[Status] in('Settled', 'SentToDebtCollection')
	and	n.ClosedTransactionDate is null
)");
        }

        public override void Down()
        {
        }
    }
}
