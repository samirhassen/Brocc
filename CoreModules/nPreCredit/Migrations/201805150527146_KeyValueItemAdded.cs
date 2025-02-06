namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class KeyValueItemAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.KeyValueItem",
                c => new
                {
                    Key = c.String(nullable: false, maxLength: 128),
                    KeySpace = c.String(nullable: false, maxLength: 128),
                    Value = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => new { t.Key, t.KeySpace });

        }

        public override void Down()
        {
            DropTable("dbo.KeyValueItem");
        }
    }
}
