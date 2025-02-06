namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPossibilityToUndoGroupMembershipCancellation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GroupMembershipCancellation", "UndoneById", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.GroupMembershipCancellation", "UndoneById");
        }
    }
}
