namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApplicationCancelled : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "IsCancelled", c => c.Boolean(nullable: false));
            AddColumn("dbo.CreditApplicationHeader", "CancelledDate", c => c.DateTimeOffset(precision: 7));
            AddColumn("dbo.CreditApplicationHeader", "CancelledBy", c => c.Int());
            AddColumn("dbo.CreditApplicationHeader", "CancelledState", c => c.String(maxLength: 100));
            CreateIndex("dbo.CreditApplicationHeader", "IsCancelled");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsCancelled" });
            DropColumn("dbo.CreditApplicationHeader", "CancelledState");
            DropColumn("dbo.CreditApplicationHeader", "CancelledBy");
            DropColumn("dbo.CreditApplicationHeader", "CancelledDate");
            DropColumn("dbo.CreditApplicationHeader", "IsCancelled");
        }
    }
}
