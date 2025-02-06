namespace nScheduler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedServiceRun : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ServiceRun",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TimeSlotName = c.String(maxLength: 128),
                    JobName = c.String(nullable: false, maxLength: 128),
                    StartDate = c.DateTimeOffset(nullable: false, precision: 7),
                    EndDate = c.DateTimeOffset(precision: 7),
                    EndStatus = c.String(maxLength: 128),
                    EndStatusData = c.String(),
                    TriggeredById = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.ServiceRun");
        }
    }
}
