namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedActiveGroups : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.GroupMembershipCancellation", "Id", "dbo.GroupMembership");
            DropIndex("dbo.GroupMembershipCancellation", new[] { "Id" });
            DropPrimaryKey("dbo.GroupMembershipCancellation");
            AddColumn("dbo.GroupMembershipCancellation", "GroupMembership_Id", c => c.Int(nullable: false));
            AlterColumn("dbo.GroupMembershipCancellation", "Id", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.GroupMembershipCancellation", "Id");
            CreateIndex("dbo.GroupMembershipCancellation", "GroupMembership_Id");
            AddForeignKey("dbo.GroupMembershipCancellation", "GroupMembership_Id", "dbo.GroupMembership", "Id", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("dbo.GroupMembershipCancellation", "GroupMembership_Id", "dbo.GroupMembership");
            DropIndex("dbo.GroupMembershipCancellation", new[] { "GroupMembership_Id" });
            DropPrimaryKey("dbo.GroupMembershipCancellation");
            AlterColumn("dbo.GroupMembershipCancellation", "Id", c => c.Int(nullable: false));
            DropColumn("dbo.GroupMembershipCancellation", "GroupMembership_Id");
            AddPrimaryKey("dbo.GroupMembershipCancellation", "Id");
            CreateIndex("dbo.GroupMembershipCancellation", "Id");
            AddForeignKey("dbo.GroupMembershipCancellation", "Id", "dbo.GroupMembership", "Id");
        }
    }
}
