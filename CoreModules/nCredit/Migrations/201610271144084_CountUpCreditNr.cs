namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CountUpCreditNr : DbMigration
    {
        public override void Up()
        {
            Sql("DBCC CHECKIDENT ('dbo.CreditKeySequence', reseed, 10300)");
        }

        public override void Down()
        {
        }
    }
}
