namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class addTableManualSignature : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ManualSignature",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SignatureSessionId = c.String(),
                    CreationDate = c.DateTime(nullable: false),
                    CommentText = c.String(nullable: false),
                    UnsignedDocumentArchiveUrl = c.String(),
                    IsRemoved = c.Boolean(),
                    RemovedDate = c.DateTime(),
                    IsHandled = c.Boolean(),
                    HandledDate = c.DateTime(),
                    SignedDocumentArchiveUrl = c.String(),
                    SignedDate = c.DateTime(),
                    SignicatUrl = c.String(),
                    Timestamp = c.Binary(),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.ManualSignature");
        }
    }
}
