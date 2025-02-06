namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedBusinessEvent : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BusinessEvent",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    EventType = c.String(nullable: false, maxLength: 100),
                    UserId = c.Int(nullable: false),
                    EventDate = c.DateTimeOffset(nullable: false, precision: 7),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.CustomerProperty", "CreatedByBusinessEventId", c => c.Int());
            CreateIndex("dbo.CustomerProperty", "CreatedByBusinessEventId");
            AddForeignKey("dbo.CustomerProperty", "CreatedByBusinessEventId", "dbo.BusinessEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CustomerProperty", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.CustomerProperty", new[] { "CreatedByBusinessEventId" });
            DropColumn("dbo.CustomerProperty", "CreatedByBusinessEventId");
            DropTable("dbo.BusinessEvent");
        }
    }
}
