namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemovedMalformedIndex : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.AuthenticationMechanism", "IX_UniqueUserIdentity");
        }

        public override void Down()
        {
            CreateIndex("dbo.AuthenticationMechanism", new string[] { "AuthenticationProvider", "UserIdentity" }, unique: true, name: "IX_UniqueUserIdentity");
        }
    }
}
