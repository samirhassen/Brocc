namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedWorkLists : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.WorkListFilterItem",
                c => new
                {
                    WorkListHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 128),
                    Value = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.WorkListHeaderId, t.Name })
                .ForeignKey("dbo.WorkListHeader", t => t.WorkListHeaderId, cascadeDelete: true)
                .Index(t => t.WorkListHeaderId);

            CreateTable(
                "dbo.WorkListHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ListType = c.String(nullable: false, maxLength: 128),
                    CreationDate = c.DateTime(nullable: false),
                    CreatedByUserId = c.Int(nullable: false),
                    ClosedByUserId = c.Int(),
                    ClosedDate = c.DateTime(),
                    CustomData = c.String(),
                    IsUnderConstruction = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.WorkListItem",
                c => new
                {
                    WorkListHeaderId = c.Int(nullable: false),
                    ItemId = c.String(nullable: false, maxLength: 128),
                    OrderNr = c.Int(nullable: false),
                    TakenByUserId = c.Int(),
                    TakenDate = c.DateTime(),
                    CompletedDate = c.DateTime(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => new { t.WorkListHeaderId, t.ItemId })
                .ForeignKey("dbo.WorkListHeader", t => t.WorkListHeaderId, cascadeDelete: true)
                .Index(t => new { t.WorkListHeaderId, t.OrderNr }, unique: true, name: "OrderUIdx");

            CreateTable(
                "dbo.WorkListItemProperty",
                c => new
                {
                    WorkListHeaderId = c.Int(nullable: false),
                    ItemId = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 128),
                    IsEncrypted = c.Boolean(nullable: false),
                    Value = c.String(maxLength: 128),
                })
                .PrimaryKey(t => new { t.WorkListHeaderId, t.ItemId, t.Name })
                .ForeignKey("dbo.WorkListItem", t => new { t.WorkListHeaderId, t.ItemId }, cascadeDelete: true)
                .Index(t => new { t.WorkListHeaderId, t.ItemId });

            Sql("CREATE INDEX CommentTypePerfIdx1 ON [dbo].[CreditComment] ([EventType],[CommentDate]) INCLUDE ([CreditNr])");
        }

        public override void Down()
        {
            Sql("DROP INDEX CommentTypePerfIdx1 ON [dbo].[CreditComment]");

            DropForeignKey("dbo.WorkListItem", "WorkListHeaderId", "dbo.WorkListHeader");
            DropForeignKey("dbo.WorkListItemProperty", new[] { "WorkListHeaderId", "ItemId" }, "dbo.WorkListItem");
            DropForeignKey("dbo.WorkListFilterItem", "WorkListHeaderId", "dbo.WorkListHeader");
            DropIndex("dbo.WorkListItemProperty", new[] { "WorkListHeaderId", "ItemId" });
            DropIndex("dbo.WorkListItem", "OrderUIdx");
            DropIndex("dbo.WorkListFilterItem", new[] { "WorkListHeaderId" });
            DropTable("dbo.WorkListItemProperty");
            DropTable("dbo.WorkListItem");
            DropTable("dbo.WorkListHeader");
            DropTable("dbo.WorkListFilterItem");
        }
    }
}
