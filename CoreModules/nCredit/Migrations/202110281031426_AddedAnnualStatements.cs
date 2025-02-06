namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAnnualStatements : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditAnnualStatementHeader",
                c => new
                {
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    Year = c.Int(nullable: false),
                    StatementDocumentArchiveKey = c.String(nullable: false, maxLength: 100),
                    OutgoingExportFileHeaderId = c.Int(),
                    CustomData = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => new { t.CreditNr, t.CustomerId, t.Year })
                .ForeignKey("dbo.OutgoingExportFileHeader", t => t.OutgoingExportFileHeaderId)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.OutgoingExportFileHeaderId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditAnnualStatementHeader", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.CreditAnnualStatementHeader", "OutgoingExportFileHeaderId", "dbo.OutgoingExportFileHeader");
            DropIndex("dbo.CreditAnnualStatementHeader", new[] { "OutgoingExportFileHeaderId" });
            DropIndex("dbo.CreditAnnualStatementHeader", new[] { "CreditNr" });
            DropTable("dbo.CreditAnnualStatementHeader");
        }
    }
}
