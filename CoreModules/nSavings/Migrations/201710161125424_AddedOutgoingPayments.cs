namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOutgoingPayments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingPaymentHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UniqueToken = c.String(maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    OutgoingPaymentFileHeaderId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OutgoingPaymentFileHeader", t => t.OutgoingPaymentFileHeaderId)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.CreatedByBusinessEventId)
                .Index(t => t.OutgoingPaymentFileHeaderId);

            CreateTable(
                "dbo.OutgoingPaymentHeaderItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    OutgoingPaymentId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsEncrypted = c.Boolean(nullable: false),
                    Value = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OutgoingPaymentHeader", t => t.OutgoingPaymentId, cascadeDelete: true)
                .Index(t => t.OutgoingPaymentId)
                .Index(t => t.Name);

            CreateTable(
                "dbo.OutgoingPaymentFileHeader",
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

            AddColumn("dbo.LedgerAccountTransaction", "OutgoingPaymentId", c => c.Int());
            CreateIndex("dbo.LedgerAccountTransaction", "OutgoingPaymentId");
            AddForeignKey("dbo.LedgerAccountTransaction", "OutgoingPaymentId", "dbo.OutgoingPaymentHeader", "Id");

            Sql("CREATE UNIQUE NONCLUSTERED INDEX OutgoingPaymentHeader_UniqueToken_UIdx ON dbo.OutgoingPaymentHeader(UniqueToken) WHERE UniqueToken IS NOT NULL");
        }

        public override void Down()
        {
            DropForeignKey("dbo.OutgoingPaymentHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.OutgoingPaymentFileHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.LedgerAccountTransaction", "OutgoingPaymentId", "dbo.OutgoingPaymentHeader");
            DropForeignKey("dbo.OutgoingPaymentHeader", "OutgoingPaymentFileHeaderId", "dbo.OutgoingPaymentFileHeader");
            DropForeignKey("dbo.OutgoingPaymentHeaderItem", "OutgoingPaymentId", "dbo.OutgoingPaymentHeader");
            DropIndex("dbo.OutgoingPaymentFileHeader", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.OutgoingPaymentHeaderItem", new[] { "Name" });
            DropIndex("dbo.OutgoingPaymentHeaderItem", new[] { "OutgoingPaymentId" });
            DropIndex("dbo.OutgoingPaymentHeader", new[] { "OutgoingPaymentFileHeaderId" });
            DropIndex("dbo.OutgoingPaymentHeader", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.LedgerAccountTransaction", new[] { "OutgoingPaymentId" });
            DropColumn("dbo.LedgerAccountTransaction", "OutgoingPaymentId");
            DropTable("dbo.OutgoingPaymentFileHeader");
            DropTable("dbo.OutgoingPaymentHeaderItem");
            DropTable("dbo.OutgoingPaymentHeader");
        }
    }
}
