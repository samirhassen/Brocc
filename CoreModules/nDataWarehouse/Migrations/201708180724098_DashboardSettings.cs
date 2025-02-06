namespace nDataWarehouse.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class DashboardSettings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AnalyticsSetting",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Key = c.String(nullable: false, maxLength: 100),
                    Value = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Key);

        }

        public override void Down()
        {
            DropIndex("dbo.AnalyticsSetting", new[] { "Key" });
            DropTable("dbo.AnalyticsSetting");
        }
    }
}
