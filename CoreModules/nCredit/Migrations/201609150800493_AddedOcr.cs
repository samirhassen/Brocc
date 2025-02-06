namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOcr : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditNotificationHeader", "OcrPaymentReference", c => c.String(nullable: false, maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditNotificationHeader", "OcrPaymentReference");
        }
    }
}
