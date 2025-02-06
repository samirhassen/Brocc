namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditApplicationDocuments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationDocumentHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ApplicantNr = c.Int(),
                    DocumentType = c.String(nullable: false, maxLength: 100),
                    AddedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    AddedByUserId = c.Int(nullable: false),
                    RemovedDate = c.DateTimeOffset(precision: 7),
                    RemovedByUserId = c.Int(),
                    DocumentArchiveKey = c.String(),
                    DocumentFileName = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationDocumentHeader", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationDocumentHeader", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApplicationDocumentHeader");
        }
    }
}
