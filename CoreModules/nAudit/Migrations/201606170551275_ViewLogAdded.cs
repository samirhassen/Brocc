namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ViewLogAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ViewLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ViewLocation = c.String(),
                    DataName = c.String(nullable: false, maxLength: 128),
                    DataIdType = c.String(nullable: false, maxLength: 128),
                    DataIdValue = c.String(nullable: false, maxLength: 128),
                    ViewedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ViewedByUserId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.DataName)
                .Index(t => t.DataIdType)
                .Index(t => t.DataIdValue)
                .Index(t => t.ViewedDate)
                .Index(t => t.ViewedByUserId);

        }

        public override void Down()
        {
            DropIndex("dbo.ViewLogItem", new[] { "ViewedByUserId" });
            DropIndex("dbo.ViewLogItem", new[] { "ViewedDate" });
            DropIndex("dbo.ViewLogItem", new[] { "DataIdValue" });
            DropIndex("dbo.ViewLogItem", new[] { "DataIdType" });
            DropIndex("dbo.ViewLogItem", new[] { "DataName" });
            DropTable("dbo.ViewLogItem");
        }
    }
}
