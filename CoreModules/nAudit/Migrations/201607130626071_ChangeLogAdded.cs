namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangeLogAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChangeLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ServiceName = c.String(nullable: false, maxLength: 128),
                    EntityName = c.String(nullable: false, maxLength: 128),
                    PrimaryKeyValue = c.String(),
                    PropertyName = c.String(nullable: false, maxLength: 128),
                    EncryptionKeyName = c.String(nullable: false, maxLength: 128),
                    EncryptedOldValue = c.Binary(),
                    EncryptedNewValue = c.Binary(),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ChangedByUserId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.ChangeLogItem");
        }
    }
}
