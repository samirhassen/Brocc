namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemovedAccessMethod : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.CustomerProperty", "AccessMethod");
        }

        public override void Down()
        {
            AddColumn("dbo.CustomerProperty", "AccessMethod", c => c.Int(nullable: false));
        }
    }
}
