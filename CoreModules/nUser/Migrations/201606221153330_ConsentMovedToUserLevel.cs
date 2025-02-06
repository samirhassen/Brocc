namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ConsentMovedToUserLevel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "ConsentedDate", c => c.DateTime());
            AddColumn("dbo.User", "ConsentText", c => c.String());
            DropColumn("dbo.GroupMembership", "ConsentedDate");
            DropColumn("dbo.GroupMembership", "ConsentText");
        }

        public override void Down()
        {
            AddColumn("dbo.GroupMembership", "ConsentText", c => c.String());
            AddColumn("dbo.GroupMembership", "ConsentedDate", c => c.DateTime());
            DropColumn("dbo.User", "ConsentText");
            DropColumn("dbo.User", "ConsentedDate");
        }
    }
}
