namespace nCreditReport.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CreditReportAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditReportHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditReportProviderName = c.String(nullable: false, maxLength: 100),
                    RequestDate = c.DateTimeOffset(nullable: false, precision: 7),
                    EncryptionKeyName = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.RequestDate);

            CreateTable(
                "dbo.EncryptedCreditReportItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditReportHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.Binary(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditReportHeader", t => t.CreditReportHeaderId, cascadeDelete: true)
                .Index(t => t.CreditReportHeaderId)
                .Index(t => t.Name);

            CreateTable(
                "dbo.CreditReportSearchTerm",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditReportHeaderId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditReportHeader", t => t.CreditReportHeaderId, cascadeDelete: true)
                .Index(t => new { t.Name, t.Value, t.CreditReportHeaderId }, name: "CreditReportSearchTermCoveringIndex")
                .Index(t => t.Name, name: "CreditReportSearchTermNameIndex")
                .Index(t => t.Value, name: "CreditReportSearchTermValueIndex");

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditReportSearchTerm", "CreditReportHeaderId", "dbo.CreditReportHeader");
            DropForeignKey("dbo.EncryptedCreditReportItem", "CreditReportHeaderId", "dbo.CreditReportHeader");
            DropIndex("dbo.CreditReportSearchTerm", "CreditReportSearchTermValueIndex");
            DropIndex("dbo.CreditReportSearchTerm", "CreditReportSearchTermNameIndex");
            DropIndex("dbo.CreditReportSearchTerm", "CreditReportSearchTermCoveringIndex");
            DropIndex("dbo.EncryptedCreditReportItem", new[] { "Name" });
            DropIndex("dbo.EncryptedCreditReportItem", new[] { "CreditReportHeaderId" });
            DropIndex("dbo.CreditReportHeader", new[] { "RequestDate" });
            DropTable("dbo.CreditReportSearchTerm");
            DropTable("dbo.EncryptedCreditReportItem");
            DropTable("dbo.CreditReportHeader");
        }
    }
}
