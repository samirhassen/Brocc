namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedUserSetting : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserSetting",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    CreationDate = c.DateTime(nullable: false),
                    CreatedById = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 128),
                    Value = c.String(),
                    UserId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.UserId, cascadeDelete: true)
                .Index(t => t.Name)
                .Index(t => t.UserId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.UserSetting", "UserId", "dbo.User");
            DropIndex("dbo.UserSetting", new[] { "UserId" });
            DropIndex("dbo.UserSetting", new[] { "Name" });
            DropTable("dbo.UserSetting");
        }
    }
}
