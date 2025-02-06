namespace nDataWarehouse.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ExportedReport : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ExportedReport",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ReportName = c.String(nullable: false, maxLength: 100),
                    ReportDate = c.DateTime(nullable: false),
                    GenerationTimeInMs = c.Long(),
                    ReportArchiveKey = c.String(maxLength: 100),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.ExportedReport");
        }
    }
}
