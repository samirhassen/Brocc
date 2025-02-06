namespace nAudit.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveChangeAndViewLogs : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.PersonalDataViewLogItem", new[] { "CustomerId" });
            DropIndex("dbo.PersonalDataViewLogItem", new[] { "ViewedDate" });
            DropIndex("dbo.PersonalDataViewLogItem", new[] { "ViewedByUserId" });
            DropTable("dbo.ChangeLogItem");
            DropTable("dbo.EncryptedValue");
            DropTable("dbo.PersonalDataViewLogItem");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.PersonalDataViewLogItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ViewLocation = c.String(),
                        CustomerId = c.Int(),
                        PropertyName = c.String(),
                        EncryptedPropertyValueId = c.Long(),
                        ViewedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        ViewedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EncryptedValue",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        EncryptionKeyName = c.String(nullable: false, maxLength: 100),
                        Value = c.Binary(nullable: false),
                        Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        CreatedById = c.Int(nullable: false),
                        CreatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ChangeLogItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ServiceName = c.String(nullable: false, maxLength: 128),
                        EntityName = c.String(nullable: false, maxLength: 128),
                        PrimaryKeyValue = c.String(),
                        PropertyName = c.String(nullable: false, maxLength: 128),
                        OldEncryptedValueId = c.Long(),
                        NewEncryptedValueId = c.Long(),
                        ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        ChangedByUserId = c.Int(nullable: false),
                        Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.PersonalDataViewLogItem", "ViewedByUserId");
            CreateIndex("dbo.PersonalDataViewLogItem", "ViewedDate");
            CreateIndex("dbo.PersonalDataViewLogItem", "CustomerId");
        }
    }
}
