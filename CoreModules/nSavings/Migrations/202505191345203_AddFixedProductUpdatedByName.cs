namespace nSavings.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFixedProductUpdatedByName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FixedAccountProduct", "UpdatedBy", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.FixedAccountProduct", "UpdatedBy");
        }
    }
}
