namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SystemLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Level = c.String(nullable: false, maxLength: 15),
                    EventDate = c.DateTimeOffset(nullable: false, precision: 7),
                    EventType = c.String(maxLength: 128),
                    ServiceName = c.String(maxLength: 30),
                    ServiceVersion = c.String(maxLength: 30),
                    RequestUri = c.String(maxLength: 128),
                    RemoteIp = c.String(maxLength: 30),
                    UserId = c.String(maxLength: 128),
                    Message = c.String(),
                    ExceptionMessage = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Level)
                .Index(t => t.EventDate)
                .Index(t => t.EventType);

        }

        public override void Down()
        {
            DropIndex("dbo.SystemLogItem", new[] { "EventType" });
            DropIndex("dbo.SystemLogItem", new[] { "EventDate" });
            DropIndex("dbo.SystemLogItem", new[] { "Level" });
            DropTable("dbo.SystemLogItem");
        }
    }
}
