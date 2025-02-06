namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCancellations : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationCancellation",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CancelledDate = c.DateTimeOffset(nullable: false, precision: 7),
                    CancelledState = c.String(maxLength: 100),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    WasAutomated = c.Boolean(nullable: false),
                    CancelledBy = c.Int(nullable: false),
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
            DropForeignKey("dbo.CreditApplicationCancellation", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationCancellation", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApplicationCancellation");
        }
    }
}
