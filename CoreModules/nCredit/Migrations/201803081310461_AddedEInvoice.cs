namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedEInvoice : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EInvoiceFiAction",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ActionName = c.String(nullable: false, maxLength: 128),
                    ActionDate = c.DateTime(nullable: false),
                    ActionMessage = c.String(),
                    CreatedByUserId = c.Int(nullable: false),
                    CreditNr = c.String(maxLength: 128),
                    EInvoiceFiMessageHeaderId = c.Int(),
                    HandledDate = c.DateTime(),
                    HandledByUserId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.EInvoiceFiMessageHeader", t => t.EInvoiceFiMessageHeaderId)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr)
                .Index(t => new { t.ActionName, t.HandledByUserId }, name: "ErrorListIdx")
                .Index(t => t.CreditNr)
                .Index(t => t.EInvoiceFiMessageHeaderId);

            CreateTable(
                "dbo.EInvoiceFiMessageHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ExternalMessageType = c.String(nullable: false, maxLength: 128),
                    ExternalMessageId = c.String(nullable: false, maxLength: 128),
                    CreatedByEventId = c.Int(nullable: false),
                    ExternalTimestamp = c.DateTimeOffset(nullable: false, precision: 7),
                    ImportDate = c.DateTime(nullable: false),
                    ImportedByUserId = c.Int(nullable: false),
                    ProcessedDate = c.DateTime(),
                    ProcessedByUserId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .Index(t => t.CreatedByEventId)
                .Index(t => t.ProcessedByUserId, name: "ProcessIdx");

            CreateTable(
                "dbo.EInvoiceFiMessageItem",
                c => new
                {
                    EInvoiceFiMessageHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 128),
                    Value = c.String(nullable: false),
                    IsEncrypted = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => new { t.EInvoiceFiMessageHeaderId, t.Name })
                .ForeignKey("dbo.EInvoiceFiMessageHeader", t => t.EInvoiceFiMessageHeaderId, cascadeDelete: true)
                .Index(t => t.EInvoiceFiMessageHeaderId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.EInvoiceFiMessageHeader", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.EInvoiceFiAction", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.EInvoiceFiMessageItem", "EInvoiceFiMessageHeaderId", "dbo.EInvoiceFiMessageHeader");
            DropForeignKey("dbo.EInvoiceFiAction", "EInvoiceFiMessageHeaderId", "dbo.EInvoiceFiMessageHeader");
            DropIndex("dbo.EInvoiceFiMessageItem", new[] { "EInvoiceFiMessageHeaderId" });
            DropIndex("dbo.EInvoiceFiMessageHeader", "ProcessIdx");
            DropIndex("dbo.EInvoiceFiMessageHeader", new[] { "CreatedByEventId" });
            DropIndex("dbo.EInvoiceFiAction", new[] { "EInvoiceFiMessageHeaderId" });
            DropIndex("dbo.EInvoiceFiAction", new[] { "CreditNr" });
            DropIndex("dbo.EInvoiceFiAction", "ErrorListIdx");
            DropTable("dbo.EInvoiceFiMessageItem");
            DropTable("dbo.EInvoiceFiMessageHeader");
            DropTable("dbo.EInvoiceFiAction");
        }
    }
}
