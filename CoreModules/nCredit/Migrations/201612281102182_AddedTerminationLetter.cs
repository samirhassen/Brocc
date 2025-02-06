namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTerminationLetter : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditTerminationLetterHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    PrintDate = c.DateTime(nullable: false, storeType: "date"),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    DueDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    CreditNr = c.String(maxLength: 128),
                    OutgoingCreditTerminationLetterDeliveryFileHeaderId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr)
                .ForeignKey("dbo.OutgoingCreditTerminationLetterDeliveryFileHeader", t => t.OutgoingCreditTerminationLetterDeliveryFileHeaderId)
                .Index(t => t.CreditNr)
                .Index(t => t.OutgoingCreditTerminationLetterDeliveryFileHeaderId);

            CreateTable(
                "dbo.OutgoingCreditTerminationLetterDeliveryFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    ExternalId = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.CreditDocument", "CreditTerminationLetterHeaderId", c => c.Int());
            CreateIndex("dbo.CreditDocument", "CreditTerminationLetterHeaderId");
            AddForeignKey("dbo.CreditDocument", "CreditTerminationLetterHeaderId", "dbo.CreditTerminationLetterHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditDocument", "CreditTerminationLetterHeaderId", "dbo.CreditTerminationLetterHeader");
            DropForeignKey("dbo.CreditTerminationLetterHeader", "OutgoingCreditTerminationLetterDeliveryFileHeaderId", "dbo.OutgoingCreditTerminationLetterDeliveryFileHeader");
            DropForeignKey("dbo.CreditTerminationLetterHeader", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditTerminationLetterHeader", new[] { "OutgoingCreditTerminationLetterDeliveryFileHeaderId" });
            DropIndex("dbo.CreditTerminationLetterHeader", new[] { "CreditNr" });
            DropIndex("dbo.CreditDocument", new[] { "CreditTerminationLetterHeaderId" });
            DropColumn("dbo.CreditDocument", "CreditTerminationLetterHeaderId");
            DropTable("dbo.OutgoingCreditTerminationLetterDeliveryFileHeader");
            DropTable("dbo.CreditTerminationLetterHeader");
        }
    }
}
