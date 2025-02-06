namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class OcrNrStartingPoint : DbMigration
    {
        public override void Up()
        {
            Sql("DBCC checkident ('OcrPaymentReferenceNrSequence', reseed, 22222222)");
        }

        public override void Down()
        {
        }
    }
}
