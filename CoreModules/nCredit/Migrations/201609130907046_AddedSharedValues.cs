namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSharedValues : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SharedDatedValue",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.BusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.SharedDatedValue", "BusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.SharedDatedValue", new[] { "BusinessEventId" });
            DropTable("dbo.SharedDatedValue");
        }
    }
}
