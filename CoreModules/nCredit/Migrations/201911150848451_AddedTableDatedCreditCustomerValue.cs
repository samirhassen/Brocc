namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTableDatedCreditCustomerValue : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DatedCreditCustomerValue",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                    CustomerId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.BusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.DatedCreditCustomerValue", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.DatedCreditCustomerValue", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.DatedCreditCustomerValue", new[] { "BusinessEventId" });
            DropIndex("dbo.DatedCreditCustomerValue", new[] { "CreditNr" });
            DropTable("dbo.DatedCreditCustomerValue");
        }
    }
}
