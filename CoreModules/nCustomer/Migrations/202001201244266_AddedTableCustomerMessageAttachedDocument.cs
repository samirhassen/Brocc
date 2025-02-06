namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTableCustomerMessageAttachedDocument : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerMessageAttachedDocument",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerMessageId = c.Int(nullable: false),
                    FileName = c.String(nullable: false),
                    ArchiveKey = c.String(nullable: false),
                    ContentTypeMimetype = c.String(nullable: false, maxLength: 100),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CustomerMessage", t => t.CustomerMessageId, cascadeDelete: true)
                .Index(t => t.CustomerMessageId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CustomerMessageAttachedDocument", "CustomerMessageId", "dbo.CustomerMessage");
            DropIndex("dbo.CustomerMessageAttachedDocument", new[] { "CustomerMessageId" });
            DropTable("dbo.CustomerMessageAttachedDocument");
        }
    }
}
