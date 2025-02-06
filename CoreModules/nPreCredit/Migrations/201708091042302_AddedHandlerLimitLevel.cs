namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedHandlerLimitLevel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HandlerLimitLevel",
                c => new
                {
                    HandlerUserId = c.Int(nullable: false),
                    LimitLevel = c.Int(nullable: false),
                    IsOverrideAllowed = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.HandlerUserId);

        }

        public override void Down()
        {
            DropTable("dbo.HandlerLimitLevel");
        }
    }
}
