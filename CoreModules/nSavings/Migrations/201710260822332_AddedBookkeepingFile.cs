namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedBookkeepingFile : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingBookkeepingFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FromTransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    ToTransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    XlsFileArchiveKey = c.String(maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.LedgerAccountTransaction", "OutgoingBookkeepingFileHeaderId", c => c.Int());
            CreateIndex("dbo.LedgerAccountTransaction", "OutgoingBookkeepingFileHeaderId");
            AddForeignKey("dbo.LedgerAccountTransaction", "OutgoingBookkeepingFileHeaderId", "dbo.OutgoingBookkeepingFileHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.LedgerAccountTransaction", "OutgoingBookkeepingFileHeaderId", "dbo.OutgoingBookkeepingFileHeader");
            DropIndex("dbo.LedgerAccountTransaction", new[] { "OutgoingBookkeepingFileHeaderId" });
            DropColumn("dbo.LedgerAccountTransaction", "OutgoingBookkeepingFileHeaderId");
            DropTable("dbo.OutgoingBookkeepingFileHeader");
        }
    }
}
