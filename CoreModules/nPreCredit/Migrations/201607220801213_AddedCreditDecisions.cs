namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditDecisions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditDecision",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    DecisionDate = c.DateTimeOffset(nullable: false, precision: 7),
                    DecisionById = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                    AcceptedDecisionModel = c.String(),
                    RejectedDecisionModel = c.String(),
                    Discriminator = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr)
                .Index(t => t.DecisionDate);

            AddColumn("dbo.CreditApplicationHeader", "CurrentCreditDecisionId", c => c.Int());
            CreateIndex("dbo.CreditApplicationHeader", "CurrentCreditDecisionId");
            AddForeignKey("dbo.CreditApplicationHeader", "CurrentCreditDecisionId", "dbo.CreditDecision", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationHeader", "CurrentCreditDecisionId", "dbo.CreditDecision");
            DropForeignKey("dbo.CreditDecision", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditDecision", new[] { "DecisionDate" });
            DropIndex("dbo.CreditDecision", new[] { "ApplicationNr" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "CurrentCreditDecisionId" });
            DropColumn("dbo.CreditApplicationHeader", "CurrentCreditDecisionId");
            DropTable("dbo.CreditDecision");
        }
    }
}
