namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPossibilityToDisapproveGroupMemberships : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GroupMembership", "DisapprovedDate", c => c.DateTime());
        }

        public override void Down()
        {
            DropColumn("dbo.GroupMembership", "DisapprovedDate");
        }
    }
}
