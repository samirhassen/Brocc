namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemovedSizeLimitFromSatExportStatus : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.OutgoingSatExportFileHeader", "ExportResultStatus", c => c.String());
        }

        public override void Down()
        {
            AlterColumn("dbo.OutgoingSatExportFileHeader", "ExportResultStatus", c => c.String(maxLength: 100));
        }
    }
}
