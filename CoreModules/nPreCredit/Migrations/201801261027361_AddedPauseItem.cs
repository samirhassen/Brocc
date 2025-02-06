namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPauseItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditDecisionPauseItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    RejectionReasonName = c.String(maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    PausedUntilDate = c.DateTime(nullable: false),
                    CreditDecisionId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditDecision", t => t.CreditDecisionId, cascadeDelete: true)
                .Index(t => t.CreditDecisionId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditDecisionPauseItem", "CreditDecisionId", "dbo.CreditDecision");
            DropIndex("dbo.CreditDecisionPauseItem", new[] { "CreditDecisionId" });
            DropTable("dbo.CreditDecisionPauseItem");
        }
    }
}
