namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSystemItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SystemItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Key = c.String(nullable: false, maxLength: 100),
                    Value = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Key);

        }

        public override void Down()
        {
            DropIndex("dbo.SystemItem", new[] { "Key" });
            DropTable("dbo.SystemItem");
        }
    }
}
