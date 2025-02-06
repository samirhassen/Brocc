namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAmlMonitoringExport : DbMigration
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

        }

        public override void Down()
        {
            DropTable("dbo.OutgoingAmlMonitoringExportFileHeader");
        }
    }
}
