namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditDecisionSearchTerms : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditDecisionSearchTerm",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TermName = c.String(nullable: false, maxLength: 128),
                    TermValue = c.String(nullable: false, maxLength: 128),
                    CreditDecisionId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditDecision", t => t.CreditDecisionId, cascadeDelete: true)
                .Index(t => new { t.TermName, t.TermValue }, name: "CreditDecisionSearchTermSearchIdx1")
                .Index(t => t.CreditDecisionId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditDecisionSearchTerm", "CreditDecisionId", "dbo.CreditDecision");
            DropIndex("dbo.CreditDecisionSearchTerm", new[] { "CreditDecisionId" });
            DropIndex("dbo.CreditDecisionSearchTerm", "CreditDecisionSearchTermSearchIdx1");
            DropTable("dbo.CreditDecisionSearchTerm");
        }
    }
}
