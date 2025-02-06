namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemovedUnusedProperties : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.User", "IsSystemUser");
            DropColumn("dbo.GroupMembership", "IsIntegration");
        }

        public override void Down()
        {
            AddColumn("dbo.GroupMembership", "IsIntegration", c => c.Boolean(nullable: false));
            AddColumn("dbo.User", "IsSystemUser", c => c.Boolean(nullable: false));
        }
    }
}
