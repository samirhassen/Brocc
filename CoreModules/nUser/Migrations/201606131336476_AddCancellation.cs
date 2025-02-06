namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddCancellation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GroupMembershipCancellation",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CancellationBeginDate = c.DateTime(nullable: false),
                    CancellationEndDate = c.DateTime(),
                    BegunById = c.Int(nullable: false),
                    CommittedById = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GroupMembership", t => t.Id)
                .Index(t => t.Id);

            Sql("insert into dbo.GroupMembershipCancellation (CancellationBeginDate, CancellationEndDate, BegunById, CommittedById) select g.CanceledDate, g.CanceledDate, g.CanceledById, g.CanceledById from dbo.GroupMembership g where g.CanceledDate is not null");

            DropColumn("dbo.GroupMembership", "CanceledDate");
            DropColumn("dbo.GroupMembership", "CanceledById");
        }

        public override void Down()
        {
            AddColumn("dbo.GroupMembership", "CanceledById", c => c.Int());
            AddColumn("dbo.GroupMembership", "CanceledDate", c => c.DateTime());
            DropForeignKey("dbo.GroupMembershipCancellation", "Id", "dbo.GroupMembership");
            DropIndex("dbo.GroupMembershipCancellation", new[] { "Id" });
            DropTable("dbo.GroupMembershipCancellation");
        }
    }
}
