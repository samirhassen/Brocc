namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditAppItemPerfIndices : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE INDEX [CreditApplicationItemReplIdx1] ON [dbo].[CreditApplicationItem] ([Timestamp]) INCLUDE ([ApplicationNr])");
        }

        public override void Down()
        {
            Sql("DROP INDEX [CreditApplicationItemReplIdx1] ON [dbo].[CreditApplicationItem]");
        }
    }
}
