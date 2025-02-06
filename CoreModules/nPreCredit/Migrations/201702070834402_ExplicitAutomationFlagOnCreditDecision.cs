namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ExplicitAutomationFlagOnCreditDecision : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditDecision", "WasAutomated", c => c.Boolean(nullable: false, defaultValue: false));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditDecision", "WasAutomated");
        }
    }
}
