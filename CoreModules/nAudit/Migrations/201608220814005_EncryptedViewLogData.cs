namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class EncryptedViewLogData : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.ViewLogItem", new[] { "DataIdValue" });
            AddColumn("dbo.ViewLogItem", "EncryptedValueId", c => c.Long());
            DropColumn("dbo.ChangeLogItem", "EncryptionKeyName");
            DropColumn("dbo.ViewLogItem", "DataIdValue");
        }

        public override void Down()
        {
            AddColumn("dbo.ViewLogItem", "DataIdValue", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.ChangeLogItem", "EncryptionKeyName", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.ViewLogItem", "EncryptedValueId");
            CreateIndex("dbo.ViewLogItem", "DataIdValue");
        }
    }
}
