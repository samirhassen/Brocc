namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AdddedEncryptedValue : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EncryptedValue",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    EncryptionKeyName = c.String(nullable: false, maxLength: 100),
                    Value = c.Binary(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    CreatedById = c.Int(nullable: false),
                    CreatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.EncryptedValue");
        }
    }
}
