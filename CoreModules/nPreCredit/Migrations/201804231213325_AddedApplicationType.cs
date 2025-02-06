namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApplicationType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "ApplicationType", c => c.String(maxLength: 50));
            CreateIndex("dbo.CreditApplicationHeader", "ApplicationType");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "ApplicationType" });
            DropColumn("dbo.CreditApplicationHeader", "ApplicationType");
        }
    }
}
