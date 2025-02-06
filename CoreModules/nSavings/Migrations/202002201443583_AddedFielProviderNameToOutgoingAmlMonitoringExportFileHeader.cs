namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedFielProviderNameToOutgoingAmlMonitoringExportFileHeader : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OutgoingAmlMonitoringExportFileHeader", "ProviderName", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.OutgoingAmlMonitoringExportFileHeader", "ProviderName");
        }
    }
}
