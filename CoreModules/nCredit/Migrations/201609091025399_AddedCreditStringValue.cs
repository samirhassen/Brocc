namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditStringValue : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DatedCreditString",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.BusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.DatedCreditString", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.DatedCreditString", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.DatedCreditString", new[] { "BusinessEventId" });
            DropIndex("dbo.DatedCreditString", new[] { "CreditNr" });
            DropTable("dbo.DatedCreditString");
        }
    }
}
