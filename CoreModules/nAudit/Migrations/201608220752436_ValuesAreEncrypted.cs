namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ValuesAreEncrypted : DbMigration
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

            AddColumn("dbo.ChangeLogItem", "OldEncryptedValueId", c => c.Long());
            AddColumn("dbo.ChangeLogItem", "NewEncryptedValueId", c => c.Long());
            DropColumn("dbo.ChangeLogItem", "EncryptedOldValue");
            DropColumn("dbo.ChangeLogItem", "EncryptedNewValue");
        }

        public override void Down()
        {
            AddColumn("dbo.ChangeLogItem", "EncryptedNewValue", c => c.Binary());
            AddColumn("dbo.ChangeLogItem", "EncryptedOldValue", c => c.Binary());
            DropColumn("dbo.ChangeLogItem", "NewEncryptedValueId");
            DropColumn("dbo.ChangeLogItem", "OldEncryptedValueId");
            DropTable("dbo.EncryptedValue");
        }
    }
}
