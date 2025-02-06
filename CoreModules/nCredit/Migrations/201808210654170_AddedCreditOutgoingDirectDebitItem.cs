namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditOutgoingDirectDebitItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditOutgoingDirectDebitItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    CreatedByEventId = c.Int(nullable: false),
                    Operation = c.String(nullable: false, maxLength: 128),
                    BankAccountOwnerCustomerId = c.Int(),
                    BankAccountNr = c.String(maxLength: 128),
                    ClientBankGiroNr = c.String(maxLength: 128),
                    PaymentNr = c.String(maxLength: 128),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.CreatedByEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditOutgoingDirectDebitItem", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditOutgoingDirectDebitItem", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditOutgoingDirectDebitItem", new[] { "CreatedByEventId" });
            DropIndex("dbo.CreditOutgoingDirectDebitItem", new[] { "CreditNr" });
            DropTable("dbo.CreditOutgoingDirectDebitItem");
        }
    }
}
