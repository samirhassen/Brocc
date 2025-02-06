namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditOutgoingDirectDebitHeader : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingDirectDebitStatusChangeFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    ExternalId = c.String(),
                    CreatedByEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .Index(t => t.CreatedByEventId);

            AddColumn("dbo.CreditOutgoingDirectDebitItem", "OutgoingDirectDebitStatusChangeFileHeaderId", c => c.Int());
            CreateIndex("dbo.CreditOutgoingDirectDebitItem", "OutgoingDirectDebitStatusChangeFileHeaderId");
            AddForeignKey("dbo.CreditOutgoingDirectDebitItem", "OutgoingDirectDebitStatusChangeFileHeaderId", "dbo.OutgoingDirectDebitStatusChangeFileHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.OutgoingDirectDebitStatusChangeFileHeader", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditOutgoingDirectDebitItem", "OutgoingDirectDebitStatusChangeFileHeaderId", "dbo.OutgoingDirectDebitStatusChangeFileHeader");
            DropIndex("dbo.OutgoingDirectDebitStatusChangeFileHeader", new[] { "CreatedByEventId" });
            DropIndex("dbo.CreditOutgoingDirectDebitItem", new[] { "OutgoingDirectDebitStatusChangeFileHeaderId" });
            DropColumn("dbo.CreditOutgoingDirectDebitItem", "OutgoingDirectDebitStatusChangeFileHeaderId");
            DropTable("dbo.OutgoingDirectDebitStatusChangeFileHeader");
        }
    }
}
