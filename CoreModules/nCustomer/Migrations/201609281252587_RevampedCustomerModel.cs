namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RevampedCustomerModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerCardConflict",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    ChangedByUserId = c.Int(nullable: false),
                    Name = c.String(nullable: false),
                    Group = c.String(nullable: false),
                    Value = c.String(nullable: false),
                    IsSensitive = c.Boolean(nullable: false),
                    ApprovedDate = c.DateTimeOffset(precision: 7),
                    DiscardedDate = c.DateTimeOffset(precision: 7),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.CustomerProperty", "CustomerId", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "InformationProvider", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "InformationProviderAuthentication", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "Name", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.CustomerProperty", "Group", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.CustomerProperty", "IsSensitive", c => c.Boolean(nullable: false));
            AlterColumn("dbo.CustomerProperty", "ChangedByUserId", c => c.Int(nullable: false));
            DropColumn("dbo.CustomerProperty", "CivicRegNr");
            DropColumn("dbo.CustomerProperty", "InformationProviderType");
            DropColumn("dbo.CustomerProperty", "InformationProviderAuthenticationLevel");
            DropColumn("dbo.CustomerProperty", "Code");
            DropColumn("dbo.CustomerProperty", "CodeGroup");
        }

        public override void Down()
        {
            AddColumn("dbo.CustomerProperty", "CodeGroup", c => c.String());
            AddColumn("dbo.CustomerProperty", "Code", c => c.String());
            AddColumn("dbo.CustomerProperty", "InformationProviderAuthenticationLevel", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "InformationProviderType", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "CivicRegNr", c => c.String(nullable: false));
            AlterColumn("dbo.CustomerProperty", "ChangedByUserId", c => c.String(nullable: false));
            DropColumn("dbo.CustomerProperty", "IsSensitive");
            DropColumn("dbo.CustomerProperty", "Group");
            DropColumn("dbo.CustomerProperty", "Name");
            DropColumn("dbo.CustomerProperty", "InformationProviderAuthentication");
            DropColumn("dbo.CustomerProperty", "InformationProvider");
            DropColumn("dbo.CustomerProperty", "CustomerId");
            DropTable("dbo.CustomerCardConflict");
        }
    }
}
