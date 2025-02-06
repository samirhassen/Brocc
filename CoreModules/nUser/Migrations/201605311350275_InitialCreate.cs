namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuthenticationMechanism",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreationDate = c.DateTime(nullable: false),
                    CreatedById = c.Int(nullable: false),
                    RemovedDate = c.DateTime(),
                    RemovedById = c.Int(),
                    UserIdentity = c.String(nullable: false, maxLength: 128),
                    AuthenticationType = c.String(nullable: false, maxLength: 128),
                    AuthenticationProvider = c.String(nullable: false, maxLength: 128),
                    Credentials = c.String(),
                    UserId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserIdentity)
                .Index(t => new { t.AuthenticationProvider, t.UserIdentity }, unique: true, name: "IX_UniqueUserIdentity")
                .Index(t => t.AuthenticationType)
                .Index(t => t.AuthenticationProvider)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.User",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreationDate = c.DateTime(nullable: false),
                    CreatedById = c.Int(nullable: false),
                    DisplayName = c.String(nullable: false, maxLength: 100),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.GroupMembership",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreationDate = c.DateTime(nullable: false),
                    CreatedById = c.Int(nullable: false),
                    ForProduct = c.String(nullable: false, maxLength: 100),
                    GroupName = c.String(nullable: false, maxLength: 100),
                    StartDate = c.DateTime(nullable: false),
                    EndDate = c.DateTime(nullable: false),
                    IsReadOnly = c.Boolean(nullable: false),
                    IsIntegration = c.Boolean(nullable: false),
                    IsApproved = c.Boolean(nullable: false),
                    IsCanceled = c.Boolean(nullable: false),
                    HasConsented = c.Boolean(nullable: false),
                    ConsentText = c.String(),
                    User_Id = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.User_Id, cascadeDelete: true)
                .Index(t => t.User_Id);

        }

        public override void Down()
        {
            DropForeignKey("dbo.AuthenticationMechanism", "UserId", "dbo.User");
            DropForeignKey("dbo.GroupMembership", "User_Id", "dbo.User");
            DropIndex("dbo.GroupMembership", new[] { "User_Id" });
            DropIndex("dbo.AuthenticationMechanism", new[] { "UserId" });
            DropIndex("dbo.AuthenticationMechanism", new[] { "AuthenticationProvider" });
            DropIndex("dbo.AuthenticationMechanism", new[] { "AuthenticationType" });
            DropIndex("dbo.AuthenticationMechanism", "IX_UniqueUserIdentity");
            DropIndex("dbo.AuthenticationMechanism", new[] { "UserIdentity" });
            DropTable("dbo.GroupMembership");
            DropTable("dbo.User");
            DropTable("dbo.AuthenticationMechanism");
        }
    }
}
