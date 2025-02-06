namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerComment : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerComment",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    CommentDate = c.DateTimeOffset(nullable: false, precision: 7),
                    Attachment = c.String(),
                    CommentById = c.Int(nullable: false),
                    EventType = c.String(maxLength: 100),
                    CommentText = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.CustomerId);

        }

        public override void Down()
        {
            DropIndex("dbo.CustomerComment", new[] { "CustomerId" });
            DropTable("dbo.CustomerComment");
        }
    }
}
