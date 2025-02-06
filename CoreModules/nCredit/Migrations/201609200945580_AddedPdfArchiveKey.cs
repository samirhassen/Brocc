namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPdfArchiveKey : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditNotificationHeader", "PdfArchiveKey", c => c.String(maxLength: 100));
            AddColumn("dbo.CreditNotificationHeader", "SentForDeliveryDate", c => c.DateTimeOffset(precision: 7));
            CreateIndex("dbo.CreditNotificationHeader", "SentForDeliveryDate");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditNotificationHeader", new[] { "SentForDeliveryDate" });
            DropColumn("dbo.CreditNotificationHeader", "SentForDeliveryDate");
            DropColumn("dbo.CreditNotificationHeader", "PdfArchiveKey");
        }
    }
}
