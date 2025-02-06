namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Infrastructuremetadataandtimestamp : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CustomerCardConflict", "Timestamp", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.CustomerCardConflict", "ChangedById", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerCardConflict", "ChangedDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AddColumn("dbo.CustomerCardConflict", "InformationMetaData", c => c.String());
            AddColumn("dbo.CustomerProperty", "Timestamp", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.CustomerProperty", "ChangedById", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "ChangedDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AddColumn("dbo.CustomerProperty", "InformationMetaData", c => c.String());
            DropColumn("dbo.CustomerCardConflict", "ChangedByUserId");
            DropColumn("dbo.CustomerProperty", "CreationDate");
            DropColumn("dbo.CustomerProperty", "ChangedByUserId");
            DropColumn("dbo.CustomerProperty", "InformationProvider");
            DropColumn("dbo.CustomerProperty", "InformationProviderAuthentication");
            DropColumn("dbo.CustomerProperty", "InformationProviderId");
        }

        public override void Down()
        {
            AddColumn("dbo.CustomerProperty", "InformationProviderId", c => c.String(nullable: false));
            AddColumn("dbo.CustomerProperty", "InformationProviderAuthentication", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "InformationProvider", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "ChangedByUserId", c => c.Int(nullable: false));
            AddColumn("dbo.CustomerProperty", "CreationDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.CustomerCardConflict", "ChangedByUserId", c => c.Int(nullable: false));
            DropColumn("dbo.CustomerProperty", "InformationMetaData");
            DropColumn("dbo.CustomerProperty", "ChangedDate");
            DropColumn("dbo.CustomerProperty", "ChangedById");
            DropColumn("dbo.CustomerProperty", "Timestamp");
            DropColumn("dbo.CustomerCardConflict", "InformationMetaData");
            DropColumn("dbo.CustomerCardConflict", "ChangedDate");
            DropColumn("dbo.CustomerCardConflict", "ChangedById");
            DropColumn("dbo.CustomerCardConflict", "Timestamp");
        }
    }
}
