namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerProperty",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreationDate = c.DateTime(nullable: false),
                    ChangedByUserId = c.String(nullable: false),
                    CivicRegNr = c.String(nullable: false),
                    Value = c.String(nullable: false),
                    AccessMethod = c.Int(nullable: false),
                    InformationProviderType = c.Int(nullable: false),
                    InformationProviderAuthenticationLevel = c.Int(nullable: false),
                    InformationProviderId = c.String(nullable: false),
                    Code = c.String(),
                    CodeGroup = c.String(),
                    IsCurrentData = c.Boolean(nullable: false),
                    ReplacesCustomerProperty_Id = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CustomerProperty", t => t.ReplacesCustomerProperty_Id)
                .Index(t => t.ReplacesCustomerProperty_Id);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CustomerProperty", "ReplacesCustomerProperty_Id", "dbo.CustomerProperty");
            DropIndex("dbo.CustomerProperty", new[] { "ReplacesCustomerProperty_Id" });
            DropTable("dbo.CustomerProperty");
        }
    }
}
