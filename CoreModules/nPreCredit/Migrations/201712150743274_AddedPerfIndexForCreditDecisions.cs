namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPerfIndexForCreditDecisions : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX CreditDecisionPerfIdx1 ON [dbo].[CreditDecision] ([Discriminator]) INCLUDE ([Id],[WasAutomated])");
        }

        public override void Down()
        {
            Sql("DROP INDEX CreditDecisionPerfIdx1 ON [dbo].[CreditDecision] ([Discriminator]) INCLUDE ([Id],[WasAutomated])");
        }
    }
}
