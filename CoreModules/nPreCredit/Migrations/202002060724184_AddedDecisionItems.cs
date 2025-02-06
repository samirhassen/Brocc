namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedDecisionItems : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditDecisionItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ItemName = c.String(nullable: false, maxLength: 128),
                    IsRepeatable = c.Boolean(nullable: false),
                    Value = c.String(nullable: false, maxLength: 256),
                    CreditDecisionId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditDecision", t => t.CreditDecisionId, cascadeDelete: true)
                .Index(t => t.CreditDecisionId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditDecisionItem", "CreditDecisionId", "dbo.CreditDecision");
            DropIndex("dbo.CreditDecisionItem", new[] { "CreditDecisionId" });
            DropTable("dbo.CreditDecisionItem");
        }
    }
}
