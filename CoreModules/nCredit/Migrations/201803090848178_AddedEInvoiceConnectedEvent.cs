namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedEInvoiceConnectedEvent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.EInvoiceFiAction", "ConnectedBusinessEventId", c => c.Int());
            CreateIndex("dbo.EInvoiceFiAction", "ConnectedBusinessEventId");
            AddForeignKey("dbo.EInvoiceFiAction", "ConnectedBusinessEventId", "dbo.BusinessEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.EInvoiceFiAction", "ConnectedBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.EInvoiceFiAction", new[] { "ConnectedBusinessEventId" });
            DropColumn("dbo.EInvoiceFiAction", "ConnectedBusinessEventId");
        }
    }
}
