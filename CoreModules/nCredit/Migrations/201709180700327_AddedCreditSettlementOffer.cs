namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditSettlementOffer : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditSettlementOfferHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    ExpectedSettlementDate = c.DateTime(nullable: false, storeType: "date"),
                    AutoExpireDate = c.DateTime(storeType: "date"),
                    CreatedByEventId = c.Int(nullable: false),
                    CommitedByEventId = c.Int(),
                    CancelledByEventId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CancelledByEventId)
                .ForeignKey("dbo.BusinessEvent", t => t.CommitedByEventId)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId)
                .Index(t => t.CreditNr)
                .Index(t => t.CreatedByEventId)
                .Index(t => t.CommitedByEventId)
                .Index(t => t.CancelledByEventId);

            CreateTable(
                "dbo.CreditSettlementOfferItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreatedByEventId = c.Int(nullable: false),
                    CreditSettlementOfferHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .ForeignKey("dbo.CreditSettlementOfferHeader", t => t.CreditSettlementOfferHeaderId, cascadeDelete: true)
                .Index(t => t.CreatedByEventId)
                .Index(t => t.CreditSettlementOfferHeaderId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditSettlementOfferHeader", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditSettlementOfferHeader", "CommitedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditSettlementOfferHeader", "CancelledByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditSettlementOfferHeader", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.CreditSettlementOfferItem", "CreditSettlementOfferHeaderId", "dbo.CreditSettlementOfferHeader");
            DropForeignKey("dbo.CreditSettlementOfferItem", "CreatedByEventId", "dbo.BusinessEvent");
            DropIndex("dbo.CreditSettlementOfferItem", new[] { "CreditSettlementOfferHeaderId" });
            DropIndex("dbo.CreditSettlementOfferItem", new[] { "CreatedByEventId" });
            DropIndex("dbo.CreditSettlementOfferHeader", new[] { "CancelledByEventId" });
            DropIndex("dbo.CreditSettlementOfferHeader", new[] { "CommitedByEventId" });
            DropIndex("dbo.CreditSettlementOfferHeader", new[] { "CreatedByEventId" });
            DropIndex("dbo.CreditSettlementOfferHeader", new[] { "CreditNr" });
            DropTable("dbo.CreditSettlementOfferItem");
            DropTable("dbo.CreditSettlementOfferHeader");
        }
    }
}
