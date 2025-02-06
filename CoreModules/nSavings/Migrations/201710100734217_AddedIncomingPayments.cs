namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedIncomingPayments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IncomingPaymentHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    IsFullyPlaced = c.Boolean(nullable: false),
                    IncomingPaymentFileId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.IncomingPaymentFileHeader", t => t.IncomingPaymentFileId)
                .Index(t => t.IncomingPaymentFileId);

            CreateTable(
                "dbo.IncomingPaymentFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    ExternalId = c.String(),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.CreatedByBusinessEventId);

            CreateTable(
                "dbo.IncomingPaymentHeaderItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    IncomingPaymentHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsEncrypted = c.Boolean(nullable: false),
                    Value = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.IncomingPaymentHeader", t => t.IncomingPaymentHeaderId, cascadeDelete: true)
                .Index(t => t.IncomingPaymentHeaderId)
                .Index(t => t.Name);

            AddColumn("dbo.LedgerAccountTransaction", "IncomingPaymentId", c => c.Int());
            CreateIndex("dbo.LedgerAccountTransaction", "IncomingPaymentId");
            AddForeignKey("dbo.LedgerAccountTransaction", "IncomingPaymentId", "dbo.IncomingPaymentHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.IncomingPaymentFileHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.LedgerAccountTransaction", "IncomingPaymentId", "dbo.IncomingPaymentHeader");
            DropForeignKey("dbo.IncomingPaymentHeaderItem", "IncomingPaymentHeaderId", "dbo.IncomingPaymentHeader");
            DropForeignKey("dbo.IncomingPaymentHeader", "IncomingPaymentFileId", "dbo.IncomingPaymentFileHeader");
            DropIndex("dbo.IncomingPaymentHeaderItem", new[] { "Name" });
            DropIndex("dbo.IncomingPaymentHeaderItem", new[] { "IncomingPaymentHeaderId" });
            DropIndex("dbo.IncomingPaymentFileHeader", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.IncomingPaymentHeader", new[] { "IncomingPaymentFileId" });
            DropIndex("dbo.LedgerAccountTransaction", new[] { "IncomingPaymentId" });
            DropColumn("dbo.LedgerAccountTransaction", "IncomingPaymentId");
            DropTable("dbo.IncomingPaymentHeaderItem");
            DropTable("dbo.IncomingPaymentFileHeader");
            DropTable("dbo.IncomingPaymentHeader");
        }
    }
}
