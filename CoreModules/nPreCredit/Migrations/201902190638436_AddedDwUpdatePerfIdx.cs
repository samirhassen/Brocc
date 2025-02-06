namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedDwUpdatePerfIdx : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX TsIdx1 ON [dbo].[CreditDecision] ([Timestamp])");
        }

        public override void Down()
        {
            Sql("DROP INDEX TsIdx1 ON [dbo].[CreditDecision]");
        }
    }
}
