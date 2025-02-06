namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerIdToApplicationDocument : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationDocumentHeader", "CustomerId", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationDocumentHeader", "CustomerId");
        }
    }
}
