namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCoNotificationFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditNotificationHeader", "IsCoNotificationMaster", c => c.Boolean());
            AddColumn("dbo.CreditNotificationHeader", "CoNotificationId", c => c.String(maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditNotificationHeader", "CoNotificationId");
            DropColumn("dbo.CreditNotificationHeader", "IsCoNotificationMaster");
        }
    }
}
