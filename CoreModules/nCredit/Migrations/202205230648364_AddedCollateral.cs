namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCollateral : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CollateralHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CollateralType = c.String(nullable: false, maxLength: 128),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId)
                .Index(t => t.CreatedByBusinessEventId);

            CreateTable(
                "dbo.CollateralItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CollateralHeaderId = c.Int(nullable: false),
                    ItemName = c.String(nullable: false, maxLength: 128),
                    StringValue = c.String(nullable: false),
                    NumericValue = c.Decimal(precision: 18, scale: 2),
                    DateValue = c.DateTime(),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    RemovedByBusinessEventId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CollateralHeader", t => t.CollateralHeaderId, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId)
                .ForeignKey("dbo.BusinessEvent", t => t.RemovedByBusinessEventId)
                .Index(t => t.CollateralHeaderId)
                .Index(t => t.CreatedByBusinessEventId)
                .Index(t => t.RemovedByBusinessEventId);

            AddColumn("dbo.CreditHeader", "CollateralHeaderId", c => c.Int());
            CreateIndex("dbo.CreditHeader", "CollateralHeaderId");
            AddForeignKey("dbo.CreditHeader", "CollateralHeaderId", "dbo.CollateralHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CollateralItem", "RemovedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CollateralHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CollateralItem", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditHeader", "CollateralHeaderId", "dbo.CollateralHeader");
            DropForeignKey("dbo.CollateralItem", "CollateralHeaderId", "dbo.CollateralHeader");
            DropIndex("dbo.CollateralItem", new[] { "RemovedByBusinessEventId" });
            DropIndex("dbo.CollateralItem", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.CollateralItem", new[] { "CollateralHeaderId" });
            DropIndex("dbo.CollateralHeader", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.CreditHeader", new[] { "CollateralHeaderId" });
            DropColumn("dbo.CreditHeader", "CollateralHeaderId");
            DropTable("dbo.CollateralItem");
            DropTable("dbo.CollateralHeader");
        }
    }
}
