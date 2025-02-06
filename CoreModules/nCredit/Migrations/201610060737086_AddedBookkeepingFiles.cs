namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedBookkeepingFiles : DbMigration
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
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.AccountTransaction", "OutgoingBookkeepingFileHeaderId", c => c.Int());
            CreateIndex("dbo.AccountTransaction", "OutgoingBookkeepingFileHeaderId");
            AddForeignKey("dbo.AccountTransaction", "OutgoingBookkeepingFileHeaderId", "dbo.OutgoingBookkeepingFileHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.AccountTransaction", "OutgoingBookkeepingFileHeaderId", "dbo.OutgoingBookkeepingFileHeader");
            DropIndex("dbo.AccountTransaction", new[] { "OutgoingBookkeepingFileHeaderId" });
            DropColumn("dbo.AccountTransaction", "OutgoingBookkeepingFileHeaderId");
            DropTable("dbo.OutgoingBookkeepingFileHeader");
        }
    }
}
