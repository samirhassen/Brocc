namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedNotificationBalanceDwUpdateIndex : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX IX_CreditNotificationId2 ON [dbo].[AccountTransaction] ([CreditNotificationId],[BusinessEventId]) INCLUDE ([TransactionDate])");
        }

        public override void Down()
        {
            Sql("DROP INDEX IX_CreditNotificationId2 ON [dbo].[AccountTransaction]");
        }
    }
}
