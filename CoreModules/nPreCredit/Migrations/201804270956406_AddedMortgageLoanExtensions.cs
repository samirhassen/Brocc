namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedMortgageLoanExtensions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationEvent",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    EventType = c.String(nullable: false, maxLength: 100),
                    EventDate = c.DateTimeOffset(nullable: false, precision: 7),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr);

            CreateTable(
                "dbo.MortgageLoanCreditApplicationHeaderExtension",
                c => new
                {
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    CustomerOfferStatus = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.ApplicationNr)
                .ForeignKey("dbo.CreditApplicationEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr)
                .Index(t => t.ApplicationNr)
                .Index(t => t.CreatedByBusinessEventId)
                .Index(t => t.CustomerOfferStatus);

        }

        public override void Down()
        {
            DropForeignKey("dbo.MortgageLoanCreditApplicationHeaderExtension", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApplicationEvent", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.MortgageLoanCreditApplicationHeaderExtension", "CreatedByBusinessEventId", "dbo.CreditApplicationEvent");
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "CustomerOfferStatus" });
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "ApplicationNr" });
            DropIndex("dbo.CreditApplicationEvent", new[] { "ApplicationNr" });
            DropTable("dbo.MortgageLoanCreditApplicationHeaderExtension");
            DropTable("dbo.CreditApplicationEvent");
        }
    }
}
