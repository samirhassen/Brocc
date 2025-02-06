namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemovedReadOnly : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.GroupMembership", "IsReadOnly");
        }

        public override void Down()
        {
            AddColumn("dbo.GroupMembership", "IsReadOnly", c => c.Boolean(nullable: false));
        }
    }
}
