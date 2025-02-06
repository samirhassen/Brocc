namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedIndices : DbMigration
    {
        public override void Up()
        {
            this.Sql("CREATE NONCLUSTERED INDEX CreditApplicationItemIdx1 ON [dbo].[CreditApplicationItem] ([Name],[IsEncrypted]) INCLUDE ([ApplicationNr],[Value])");
        }

        public override void Down()
        {
            this.Sql("DROP INDEX CreditApplicationItemIdx1 ON [dbo].[CreditApplicationItem]");
        }
    }
}
