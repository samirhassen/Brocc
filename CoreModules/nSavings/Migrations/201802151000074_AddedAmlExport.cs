namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAmlExport : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingAmlMonitoringExportFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    ExportResultStatus = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.SystemItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Key = c.String(nullable: false, maxLength: 100),
                    Value = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Key);

        }

        public override void Down()
        {
            DropIndex("dbo.SystemItem", new[] { "Key" });
            DropTable("dbo.SystemItem");
            DropTable("dbo.OutgoingAmlMonitoringExportFileHeader");
        }
    }
}
