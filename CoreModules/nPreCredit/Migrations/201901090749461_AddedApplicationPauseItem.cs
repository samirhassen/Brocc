namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApplicationPauseItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationPauseItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    PauseReasonName = c.String(nullable: false, maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    PausedUntilDate = c.DateTime(nullable: false),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    RemovedBy = c.Int(),
                    RemovedDate = c.DateTimeOffset(precision: 7),
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
            DropForeignKey("dbo.CreditApplicationPauseItem", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationPauseItem", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApplicationPauseItem");
        }
    }
}
