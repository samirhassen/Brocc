namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditTermsChange : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditTermsChangeItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreatedByEventId = c.Int(nullable: false),
                    CreditTermsChangeHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    ApplicantNr = c.Int(),
                    Value = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditTermsChangeHeader", t => t.CreditTermsChangeHeaderId, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .Index(t => t.CreatedByEventId)
                .Index(t => t.CreditTermsChangeHeaderId);

            CreateTable(
                "dbo.CreditTermsChangeHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    AutoExpireDate = c.DateTime(),
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

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditTermsChangeHeader", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditTermsChangeHeader", "CommitedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditTermsChangeHeader", "CancelledByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditTermsChangeItem", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditTermsChangeItem", "CreditTermsChangeHeaderId", "dbo.CreditTermsChangeHeader");
            DropForeignKey("dbo.CreditTermsChangeHeader", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditTermsChangeHeader", new[] { "CancelledByEventId" });
            DropIndex("dbo.CreditTermsChangeHeader", new[] { "CommitedByEventId" });
            DropIndex("dbo.CreditTermsChangeHeader", new[] { "CreatedByEventId" });
            DropIndex("dbo.CreditTermsChangeHeader", new[] { "CreditNr" });
            DropIndex("dbo.CreditTermsChangeItem", new[] { "CreditTermsChangeHeaderId" });
            DropIndex("dbo.CreditTermsChangeItem", new[] { "CreatedByEventId" });
            DropTable("dbo.CreditTermsChangeHeader");
            DropTable("dbo.CreditTermsChangeItem");
        }
    }
}
