namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class UsersAddedSystemUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "IsSystemUser", c => c.Boolean(nullable: false, defaultValue: false));
        }

        public override void Down()
        {
            DropColumn("dbo.User", "IsSystemUser");
        }
    }
}
