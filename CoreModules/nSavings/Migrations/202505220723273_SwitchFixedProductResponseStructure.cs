namespace nSavings.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SwitchFixedProductResponseStructure : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.FixedAccountProduct", name: "ApprovedAtBusinessEvent_Id", newName: "RespondedAtBusinessEvent_Id");
            RenameIndex(table: "dbo.FixedAccountProduct", name: "IX_ApprovedAtBusinessEvent_Id", newName: "IX_RespondedAtBusinessEvent_Id");
            AddColumn("dbo.FixedAccountProduct", "Response", c => c.Boolean());
            AddColumn("dbo.FixedAccountProduct", "RespondedAt", c => c.DateTime());
            AddColumn("dbo.FixedAccountProduct", "RespondedBy", c => c.String());
            DropColumn("dbo.FixedAccountProduct", "ApprovedAt");
            DropColumn("dbo.FixedAccountProduct", "ApprovedBy");
        }
        
        public override void Down()
        {
            AddColumn("dbo.FixedAccountProduct", "ApprovedBy", c => c.String());
            AddColumn("dbo.FixedAccountProduct", "ApprovedAt", c => c.DateTime());
            DropColumn("dbo.FixedAccountProduct", "RespondedBy");
            DropColumn("dbo.FixedAccountProduct", "RespondedAt");
            DropColumn("dbo.FixedAccountProduct", "Response");
            RenameIndex(table: "dbo.FixedAccountProduct", name: "IX_RespondedAtBusinessEvent_Id", newName: "IX_ApprovedAtBusinessEvent_Id");
            RenameColumn(table: "dbo.FixedAccountProduct", name: "RespondedAtBusinessEvent_Id", newName: "ApprovedAtBusinessEvent_Id");
        }
    }
}
