namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditDecisionType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditDecision", "DecisionType", c => c.String(maxLength: 20));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditDecision", "DecisionType");
        }
    }
}
