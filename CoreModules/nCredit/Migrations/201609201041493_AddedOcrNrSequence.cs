namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOcrNrSequence : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OcrPaymentReferenceNrSequence",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                })
                .PrimaryKey(t => t.Id);
            Sql("DBCC checkident ('OcrPaymentReferenceNrSequence', reseed, 11111111)"); //Arbitrary start value to not have it start at 1 which makes testing strange. Doesnt really matter where this starts in production
        }

        public override void Down()
        {
            DropTable("dbo.OcrPaymentReferenceNrSequence");
        }
    }
}
