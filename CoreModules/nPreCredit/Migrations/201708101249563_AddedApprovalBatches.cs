namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApprovalBatches : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApprovalBatchItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    CreditNr = c.String(maxLength: 128),
                    ApprovalType = c.String(nullable: false, maxLength: 128),
                    ApprovedAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    DecisionById = c.Int(nullable: false),
                    ApprovedById = c.Int(nullable: false),
                    CreditApprovalBatchHeaderId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApprovalBatchHeader", t => t.CreditApprovalBatchHeaderId, cascadeDelete: true)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr)
                .Index(t => t.CreditApprovalBatchHeaderId);

            CreateTable(
                "dbo.CreditApprovalBatchHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApprovedById = c.Int(nullable: false),
                    ApprovedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.CreditApprovalBatchItemOverride",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CodeName = c.String(nullable: false, maxLength: 128),
                    ContextData = c.String(),
                    CreditApprovalBatchItemId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApprovalBatchItem", t => t.CreditApprovalBatchItemId, cascadeDelete: true)
                .Index(t => t.CreditApprovalBatchItemId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApprovalBatchItem", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApprovalBatchItemOverride", "CreditApprovalBatchItemId", "dbo.CreditApprovalBatchItem");
            DropForeignKey("dbo.CreditApprovalBatchItem", "CreditApprovalBatchHeaderId", "dbo.CreditApprovalBatchHeader");
            DropIndex("dbo.CreditApprovalBatchItemOverride", new[] { "CreditApprovalBatchItemId" });
            DropIndex("dbo.CreditApprovalBatchItem", new[] { "CreditApprovalBatchHeaderId" });
            DropIndex("dbo.CreditApprovalBatchItem", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApprovalBatchItemOverride");
            DropTable("dbo.CreditApprovalBatchHeader");
            DropTable("dbo.CreditApprovalBatchItem");
        }
    }
}
