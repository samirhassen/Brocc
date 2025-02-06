namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class DroppedApplicationStatusAgain : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "ApplicationStatus" });
            DropColumn("dbo.CreditApplicationHeader", "ApplicationStatus");
        }

        public override void Down()
        {
            AddColumn("dbo.CreditApplicationHeader", "ApplicationStatus", c => c.String(nullable: false, maxLength: 50));
            CreateIndex("dbo.CreditApplicationHeader", "ApplicationStatus");
        }
    }
}
