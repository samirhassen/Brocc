namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangeViewLogToPersonalDataOnly : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.ViewLogItem", new[] { "DataName" });
            DropIndex("dbo.ViewLogItem", new[] { "DataIdType" });
            DropIndex("dbo.ViewLogItem", new[] { "ViewedDate" });
            DropIndex("dbo.ViewLogItem", new[] { "ViewedByUserId" });
            CreateTable(
                "dbo.PersonalDataViewLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ViewLocation = c.String(),
                    EncryptedCivicRegNrId = c.Long(),
                    PropertyName = c.String(),
                    EncryptedPropertyValueId = c.Long(),
                    ViewedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ViewedByUserId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.ViewedDate)
                .Index(t => t.ViewedByUserId);

            DropTable("dbo.ViewLogItem");
        }

        public override void Down()
        {
            CreateTable(
                "dbo.ViewLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ViewLocation = c.String(),
                    DataName = c.String(nullable: false, maxLength: 128),
                    DataIdType = c.String(nullable: false, maxLength: 128),
                    EncryptedValueId = c.Long(),
                    ViewedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ViewedByUserId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            DropIndex("dbo.PersonalDataViewLogItem", new[] { "ViewedByUserId" });
            DropIndex("dbo.PersonalDataViewLogItem", new[] { "ViewedDate" });
            DropTable("dbo.PersonalDataViewLogItem");
            CreateIndex("dbo.ViewLogItem", "ViewedByUserId");
            CreateIndex("dbo.ViewLogItem", "ViewedDate");
            CreateIndex("dbo.ViewLogItem", "DataIdType");
            CreateIndex("dbo.ViewLogItem", "DataName");
        }
    }
}
