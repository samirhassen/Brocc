namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedReferenceInterestChangeHeader : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReferenceInterestChangeHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    NewInterestRatePercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                    InitiatedByUserId = c.Int(nullable: false),
                    InitiatedDate = c.DateTime(nullable: false),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.CreatedByBusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.ReferenceInterestChangeHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.ReferenceInterestChangeHeader", new[] { "CreatedByBusinessEventId" });
            DropTable("dbo.ReferenceInterestChangeHeader");
        }
    }
}
