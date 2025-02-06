namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedNotificationClosedDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditNotificationHeader", "ClosedTransactionDate", c => c.DateTime(storeType: "date"));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditNotificationHeader", "ClosedTransactionDate");
        }
    }
}
