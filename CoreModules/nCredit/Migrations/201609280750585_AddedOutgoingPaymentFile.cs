namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOutgoingPaymentFile : DbMigration
    {
        public override void Up()
        {
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

            AddColumn("dbo.OutgoingPaymentHeader", "OutgoingPaymentFileHeaderId", c => c.Int());
            CreateIndex("dbo.OutgoingPaymentHeader", "OutgoingPaymentFileHeaderId");
            AddForeignKey("dbo.OutgoingPaymentHeader", "OutgoingPaymentFileHeaderId", "dbo.OutgoingPaymentFileHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.OutgoingPaymentFileHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.OutgoingPaymentHeader", "OutgoingPaymentFileHeaderId", "dbo.OutgoingPaymentFileHeader");
            DropIndex("dbo.OutgoingPaymentHeader", new[] { "OutgoingPaymentFileHeaderId" });
            DropIndex("dbo.OutgoingPaymentFileHeader", new[] { "CreatedByBusinessEventId" });
            DropColumn("dbo.OutgoingPaymentHeader", "OutgoingPaymentFileHeaderId");
            DropTable("dbo.OutgoingPaymentFileHeader");
        }
    }
}
