namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPaymentHeaderIsFullyPlaced : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentHeader", "IsFullyPlaced", c => c.Boolean(nullable: false, defaultValue: false));
            CreateIndex("dbo.PaymentHeader", "IsFullyPlaced");
        }

        public override void Down()
        {
            DropIndex("dbo.PaymentHeader", new[] { "IsFullyPlaced" });
            DropColumn("dbo.PaymentHeader", "IsFullyPlaced");
        }
    }
}
