namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemindersToAllApplicants : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditDocument",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditReminderHeaderId = c.Int(),
                    ArchiveKey = c.String(nullable: false, maxLength: 100),
                    DocumentType = c.String(nullable: false, maxLength: 100),
                    ApplicantNr = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditReminderHeader", t => t.CreditReminderHeaderId)
                .Index(t => t.CreditReminderHeaderId);

            DropColumn("dbo.CreditReminderHeader", "PdfArchiveKey");
        }

        public override void Down()
        {
            AddColumn("dbo.CreditReminderHeader", "PdfArchiveKey", c => c.String(maxLength: 100));
            DropForeignKey("dbo.CreditDocument", "CreditReminderHeaderId", "dbo.CreditReminderHeader");
            DropIndex("dbo.CreditDocument", new[] { "CreditReminderHeaderId" });
            DropTable("dbo.CreditDocument");
        }
    }
}
