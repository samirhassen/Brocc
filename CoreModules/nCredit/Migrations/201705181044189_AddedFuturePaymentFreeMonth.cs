namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedFuturePaymentFreeMonth : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditFuturePaymentFreeMonth",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ForMonth = c.DateTime(nullable: false, storeType: "date"),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    CancelledByBusinessEventId = c.Int(),
                    CommitedByEventBusinessEventId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CancelledByBusinessEventId)
                .ForeignKey("dbo.BusinessEvent", t => t.CommitedByEventBusinessEventId)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.CreatedByBusinessEventId)
                .Index(t => t.CancelledByBusinessEventId)
                .Index(t => t.CommitedByEventBusinessEventId);

            CreateTable(
                "dbo.CreditPaymentFreeMonth",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    NotificationDate = c.DateTime(nullable: false, storeType: "date"),
                    DueDate = c.DateTime(nullable: false, storeType: "date"),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.CreatedByBusinessEventId);

            AddColumn("dbo.AccountTransaction", "CreditPaymentFreeMonthId", c => c.Int());
            CreateIndex("dbo.AccountTransaction", "CreditPaymentFreeMonthId");
            AddForeignKey("dbo.AccountTransaction", "CreditPaymentFreeMonthId", "dbo.CreditPaymentFreeMonth", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditPaymentFreeMonth", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditFuturePaymentFreeMonth", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditFuturePaymentFreeMonth", "CommitedByEventBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditFuturePaymentFreeMonth", "CancelledByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditPaymentFreeMonth", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.AccountTransaction", "CreditPaymentFreeMonthId", "dbo.CreditPaymentFreeMonth");
            DropForeignKey("dbo.CreditFuturePaymentFreeMonth", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.AccountTransaction", new[] { "CreditPaymentFreeMonthId" });
            DropIndex("dbo.CreditPaymentFreeMonth", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.CreditPaymentFreeMonth", new[] { "CreditNr" });
            DropIndex("dbo.CreditFuturePaymentFreeMonth", new[] { "CommitedByEventBusinessEventId" });
            DropIndex("dbo.CreditFuturePaymentFreeMonth", new[] { "CancelledByBusinessEventId" });
            DropIndex("dbo.CreditFuturePaymentFreeMonth", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.CreditFuturePaymentFreeMonth", new[] { "CreditNr" });
            DropColumn("dbo.AccountTransaction", "CreditPaymentFreeMonthId");
            DropTable("dbo.CreditPaymentFreeMonth");
            DropTable("dbo.CreditFuturePaymentFreeMonth");
        }
    }
}
