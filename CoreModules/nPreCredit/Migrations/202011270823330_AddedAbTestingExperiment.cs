namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAbTestingExperiment : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AbTestingExperiment",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ExperimentName = c.String(nullable: false, maxLength: 256),
                    StartDate = c.DateTime(storeType: "date"),
                    EndDate = c.DateTime(storeType: "date"),
                    MaxCount = c.Int(),
                    IsActive = c.Boolean(nullable: false),
                    VariationPercent = c.Decimal(precision: 18, scale: 2),
                    VariationName = c.String(nullable: false, maxLength: 128),
                    CreatedById = c.Int(nullable: false),
                    CreatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    Timestamp = c.Binary(),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.AbTestingExperiment");
        }
    }
}
