namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class addTableManualSignature2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ManualSignature", "UnsignedDocumentArchiveKey", c => c.String());
            AddColumn("dbo.ManualSignature", "SignedDocumentArchiveKey", c => c.String());
            DropColumn("dbo.ManualSignature", "UnsignedDocumentArchiveUrl");
            DropColumn("dbo.ManualSignature", "SignedDocumentArchiveUrl");
            DropColumn("dbo.ManualSignature", "SignicatUrl");
        }

        public override void Down()
        {
            AddColumn("dbo.ManualSignature", "SignicatUrl", c => c.String());
            AddColumn("dbo.ManualSignature", "SignedDocumentArchiveUrl", c => c.String());
            AddColumn("dbo.ManualSignature", "UnsignedDocumentArchiveUrl", c => c.String());
            DropColumn("dbo.ManualSignature", "SignedDocumentArchiveKey");
            DropColumn("dbo.ManualSignature", "UnsignedDocumentArchiveKey");
        }
    }
}
