namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangedToUseTheCommonEncryptionMethod : DbMigration
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

            Sql("delete from dbo.CustomerCardConflict");
            Sql("delete from dbo.CustomerProperty");
            AddColumn("dbo.CustomerCardConflict", "IsEncrypted", c => c.Boolean(nullable: false));
            AddColumn("dbo.CustomerProperty", "IsEncrypted", c => c.Boolean(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.CustomerProperty", "IsEncrypted");
            DropColumn("dbo.CustomerCardConflict", "IsEncrypted");
            DropTable("dbo.EncryptedValue");
        }
    }
}
