namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class _20160609_2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GroupMembership", "ApprovedDate", c => c.DateTime());
            AddColumn("dbo.GroupMembership", "ApprovedById", c => c.Int());
            AddColumn("dbo.GroupMembership", "CanceledDate", c => c.DateTime());
            AddColumn("dbo.GroupMembership", "CanceledById", c => c.Int());
            AddColumn("dbo.GroupMembership", "ConsentedDate", c => c.DateTime());

            Sql("update dbo.GroupMembership set ApprovedDate = getdate(), ApprovedById = 0 where IsApproved = 1");
            Sql("update dbo.GroupMembership set CanceledDate = getdate(), CanceledById = 0 where IsCanceled = 1");
            Sql("update dbo.GroupMembership set ConsentedDate = getdate() where HasConsented = 1");

            DropColumn("dbo.GroupMembership", "IsApproved");
            DropColumn("dbo.GroupMembership", "IsCanceled");
            DropColumn("dbo.GroupMembership", "HasConsented");
        }

        public override void Down()
        {
            AddColumn("dbo.GroupMembership", "HasConsented", c => c.Boolean(nullable: false));
            AddColumn("dbo.GroupMembership", "IsCanceled", c => c.Boolean(nullable: false));
            AddColumn("dbo.GroupMembership", "IsApproved", c => c.Boolean(nullable: false));
            DropColumn("dbo.GroupMembership", "ConsentedDate");
            DropColumn("dbo.GroupMembership", "CanceledById");
            DropColumn("dbo.GroupMembership", "CanceledDate");
            DropColumn("dbo.GroupMembership", "ApprovedById");
            DropColumn("dbo.GroupMembership", "ApprovedDate");
        }
    }
}
