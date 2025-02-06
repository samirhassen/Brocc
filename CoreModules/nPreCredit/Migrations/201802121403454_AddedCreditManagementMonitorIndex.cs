namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditManagementMonitorIndex : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX CreditManagementMonitorIdx1 ON [dbo].[CreditApplicationHeader] ([ApplicationDate],[HideFromManualListsUntilDate]) INCLUDE ([ApplicationNr],[CurrentCreditDecisionId],[IsCancelled])");
        }

        public override void Down()
        {
            Sql("DROP INDEX CreditManagementMonitorIdx1 ON [dbo].[CreditApplicationHeader]");
        }
    }
}
