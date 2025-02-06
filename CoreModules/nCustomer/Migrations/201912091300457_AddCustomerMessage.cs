namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddCustomerMessage : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerMessage",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    ChannelType = c.String(nullable: false, maxLength: 100),
                    ChannelId = c.String(nullable: false, maxLength: 100),
                    IsFromCustomer = c.Boolean(nullable: false),
                    Text = c.String(nullable: false),
                    CreatedDate = c.DateTime(nullable: false, storeType: "date"),
                    CreatedByUserId = c.Int(nullable: false),
                    HandledByUserId = c.Int(),
                    HandledDate = c.DateTime(storeType: "date"),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.CustomerMessage");
        }
    }
}
