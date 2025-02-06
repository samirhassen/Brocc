namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTrapetsBacking : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TrapetsQueryResultItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TrapetsQueryResultId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false),
                    IsEncrypted = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TrapetsQueryResult", t => t.TrapetsQueryResultId, cascadeDelete: true)
                .Index(t => t.TrapetsQueryResultId)
                .Index(t => t.Name);

            CreateTable(
                "dbo.TrapetsQueryResult",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    QueryDate = c.DateTime(nullable: false, storeType: "date"),
                    IsPepHit = c.Boolean(nullable: false),
                    IsSanctionHit = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.CustomerId, t.QueryDate }, name: "TrapetsQueryResultCoveringIdx1");

        }

        public override void Down()
        {
            DropForeignKey("dbo.TrapetsQueryResultItem", "TrapetsQueryResultId", "dbo.TrapetsQueryResult");
            DropIndex("dbo.TrapetsQueryResult", "TrapetsQueryResultCoveringIdx1");
            DropIndex("dbo.TrapetsQueryResultItem", new[] { "Name" });
            DropIndex("dbo.TrapetsQueryResultItem", new[] { "TrapetsQueryResultId" });
            DropTable("dbo.TrapetsQueryResult");
            DropTable("dbo.TrapetsQueryResultItem");
        }
    }
}
