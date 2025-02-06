namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedStatusToApplicationHeader : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "ApplicationStatus", c => c.String(nullable: false, maxLength: 50, defaultValue: "Unknown"));
            CreateIndex("dbo.CreditApplicationHeader", "ApplicationStatus");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "ApplicationStatus" });
            DropColumn("dbo.CreditApplicationHeader", "ApplicationStatus");
        }
    }
}
