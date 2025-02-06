namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class SwitchedApplicatonToEncryptedValue : DbMigration
    {
        public override void Up()
        {
            Sql("delete from CreditApplicationHeader");
            DropForeignKey("dbo.EncryptedCreditApplicationItem", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.EncryptedCreditApplicationItem", new[] { "ApplicationNr" });
            DropIndex("dbo.EncryptedCreditApplicationItem", "CreditApplicationEncryptedItemNamesIndex");
            DropIndex("dbo.EncryptedCreditApplicationItem", "CreditApplicationEncryptedItemGroupNameIndex");
            DropIndex("dbo.EncryptedCreditApplicationItem", "CreditApplicationEncryptedItemNameIndex");
            AddColumn("dbo.CreditApplicationItem", "IsEncrypted", c => c.Boolean(nullable: false));
            DropColumn("dbo.CreditApplicationHeader", "EncryptionKeyName");
            DropTable("dbo.EncryptedCreditApplicationItem");
        }

        public override void Down()
        {
            CreateTable(
                "dbo.EncryptedCreditApplicationItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    GroupName = c.String(nullable: false, maxLength: 100),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.Binary(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.CreditApplicationHeader", "EncryptionKeyName", c => c.String(nullable: false));
            DropColumn("dbo.CreditApplicationItem", "IsEncrypted");
            CreateIndex("dbo.EncryptedCreditApplicationItem", "Name", name: "CreditApplicationEncryptedItemNameIndex");
            CreateIndex("dbo.EncryptedCreditApplicationItem", "GroupName", name: "CreditApplicationEncryptedItemGroupNameIndex");
            CreateIndex("dbo.EncryptedCreditApplicationItem", new[] { "GroupName", "Name" }, name: "CreditApplicationEncryptedItemNamesIndex");
            CreateIndex("dbo.EncryptedCreditApplicationItem", "ApplicationNr");
            AddForeignKey("dbo.EncryptedCreditApplicationItem", "ApplicationNr", "dbo.CreditApplicationHeader", "ApplicationNr", cascadeDelete: true);
        }
    }
}
