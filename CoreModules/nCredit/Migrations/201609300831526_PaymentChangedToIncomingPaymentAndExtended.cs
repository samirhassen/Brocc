namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class PaymentChangedToIncomingPaymentAndExtended : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.PaymentHeader", newName: "IncomingPaymentHeader");
            DropIndex("dbo.IncomingPaymentHeader", new[] { "IsFullyPlaced" });
            RenameColumn(table: "dbo.AccountTransaction", name: "PaymentId", newName: "IncomingPaymentId");
            RenameIndex(table: "dbo.AccountTransaction", name: "IX_PaymentId", newName: "IX_IncomingPaymentId");
            CreateTable(
                "dbo.IncomingPaymentFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
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

            AddColumn("dbo.IncomingPaymentHeader", "IncomingPaymentFileId", c => c.Int());
            CreateIndex("dbo.IncomingPaymentHeader", "IncomingPaymentFileId");
            AddForeignKey("dbo.IncomingPaymentHeader", "IncomingPaymentFileId", "dbo.IncomingPaymentFileHeader", "Id");
            DropColumn("dbo.IncomingPaymentHeader", "NoteText");
        }

        public override void Down()
        {
            AddColumn("dbo.IncomingPaymentHeader", "NoteText", c => c.String());
            DropForeignKey("dbo.IncomingPaymentFileHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.IncomingPaymentHeader", "IncomingPaymentFileId", "dbo.IncomingPaymentFileHeader");
            DropForeignKey("dbo.IncomingPaymentHeaderItem", "IncomingPaymentHeaderId", "dbo.IncomingPaymentHeader");
            DropIndex("dbo.IncomingPaymentHeaderItem", new[] { "Name" });
            DropIndex("dbo.IncomingPaymentHeaderItem", new[] { "IncomingPaymentHeaderId" });
            DropIndex("dbo.IncomingPaymentHeader", new[] { "IncomingPaymentFileId" });
            DropIndex("dbo.IncomingPaymentFileHeader", new[] { "CreatedByBusinessEventId" });
            DropColumn("dbo.IncomingPaymentHeader", "IncomingPaymentFileId");
            DropTable("dbo.IncomingPaymentHeaderItem");
            DropTable("dbo.IncomingPaymentFileHeader");
            RenameIndex(table: "dbo.AccountTransaction", name: "IX_IncomingPaymentId", newName: "IX_PaymentId");
            RenameColumn(table: "dbo.AccountTransaction", name: "IncomingPaymentId", newName: "PaymentId");
            CreateIndex("dbo.IncomingPaymentHeader", "IsFullyPlaced");
            RenameTable(name: "dbo.IncomingPaymentHeader", newName: "PaymentHeader");
        }
    }
}
