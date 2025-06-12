namespace nSavings.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LinkFixedProductAuditLogToBusinessEvent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FixedAccountProductAuditLog", "BusinessEvent_Id", c => c.Int());
            CreateIndex("dbo.FixedAccountProductAuditLog", "BusinessEvent_Id");
            AddForeignKey("dbo.FixedAccountProductAuditLog", "BusinessEvent_Id", "dbo.BusinessEvent", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FixedAccountProductAuditLog", "BusinessEvent_Id", "dbo.BusinessEvent");
            DropIndex("dbo.FixedAccountProductAuditLog", new[] { "BusinessEvent_Id" });
            DropColumn("dbo.FixedAccountProductAuditLog", "BusinessEvent_Id");
        }
    }
}
