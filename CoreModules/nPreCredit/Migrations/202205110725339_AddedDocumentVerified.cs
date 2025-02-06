namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedDocumentVerified : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationDocumentHeader", "VerifiedDate", c => c.DateTimeOffset(precision: 7));
            AddColumn("dbo.CreditApplicationDocumentHeader", "VerifiedByUserId", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationDocumentHeader", "VerifiedByUserId");
            DropColumn("dbo.CreditApplicationDocumentHeader", "VerifiedDate");
        }
    }
}
