namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditDocumentCustomerId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditDocument", "CustomerId", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditDocument", "CustomerId");
        }
    }
}
