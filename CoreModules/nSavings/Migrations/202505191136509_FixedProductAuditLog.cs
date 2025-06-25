namespace nSavings.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixedProductAuditLog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FixedAccountProductAuditLog",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Message = c.String(nullable: false, maxLength: 512),
                        User = c.String(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, storeType: "date"),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.FixedAccountProductAuditLog");
        }
    }
}
