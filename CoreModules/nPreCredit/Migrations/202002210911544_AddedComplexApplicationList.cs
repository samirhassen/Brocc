namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedComplexApplicationList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ComplexApplicationListItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ListName = c.String(nullable: false, maxLength: 128),
                    Nr = c.Int(nullable: false),
                    ItemName = c.String(nullable: false, maxLength: 128),
                    IsRepeatable = c.Boolean(nullable: false),
                    ItemValue = c.String(nullable: false, maxLength: 512),
                    CreatedByEventId = c.Int(nullable: false),
                    LatestChangeEventId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .ForeignKey("dbo.CreditApplicationEvent", t => t.LatestChangeEventId)
                .ForeignKey("dbo.CreditApplicationEvent", t => t.CreatedByEventId)
                .Index(t => t.ApplicationNr)
                .Index(t => t.CreatedByEventId)
                .Index(t => t.LatestChangeEventId);

            CreateTable(
                "dbo.HComplexApplicationListItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(),
                    ListName = c.String(nullable: false, maxLength: 128),
                    Nr = c.Int(nullable: false),
                    ItemName = c.String(nullable: false, maxLength: 128),
                    IsRepeatable = c.Boolean(nullable: false),
                    ItemValue = c.String(maxLength: 512),
                    ChangeEventId = c.Int(),
                    ChangeTypeCode = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationEvent", t => t.ChangeEventId)
                .Index(t => t.ChangeEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.HComplexApplicationListItem", "ChangeEventId", "dbo.CreditApplicationEvent");
            DropForeignKey("dbo.ComplexApplicationListItem", "CreatedByEventId", "dbo.CreditApplicationEvent");
            DropForeignKey("dbo.ComplexApplicationListItem", "LatestChangeEventId", "dbo.CreditApplicationEvent");
            DropForeignKey("dbo.ComplexApplicationListItem", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.HComplexApplicationListItem", new[] { "ChangeEventId" });
            DropIndex("dbo.ComplexApplicationListItem", new[] { "LatestChangeEventId" });
            DropIndex("dbo.ComplexApplicationListItem", new[] { "CreatedByEventId" });
            DropIndex("dbo.ComplexApplicationListItem", new[] { "ApplicationNr" });
            DropTable("dbo.HComplexApplicationListItem");
            DropTable("dbo.ComplexApplicationListItem");
        }
    }
}
