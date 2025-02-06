namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAffiliateReporting : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AffiliateReportingEvent",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ProviderName = c.String(nullable: false, maxLength: 128),
                    EventType = c.String(nullable: false, maxLength: 128),
                    EventData = c.String(nullable: false),
                    CreationDate = c.DateTime(nullable: false),
                    WaitUntilDate = c.DateTime(nullable: false),
                    DeleteAfterDate = c.DateTime(nullable: false),
                    ProcessedDate = c.DateTime(),
                    ProcessedStatus = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.AffiliateReportingLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    IncomingApplicationEventId = c.Long(nullable: false),
                    MessageText = c.String(maxLength: 1024),
                    ExceptionText = c.String(maxLength: 1024),
                    ProcessedStatus = c.String(nullable: false, maxLength: 128),
                    OutgoingRequestBody = c.String(),
                    OutgoingResponseBody = c.String(),
                    ProviderName = c.String(nullable: false, maxLength: 128),
                    ThrottlingContext = c.String(maxLength: 128),
                    ThrottlingCount = c.Int(),
                    LogDate = c.DateTime(nullable: false),
                    DeleteAfterDate = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.AffiliateReportingLogItem");
            DropTable("dbo.AffiliateReportingEvent");
        }
    }
}
