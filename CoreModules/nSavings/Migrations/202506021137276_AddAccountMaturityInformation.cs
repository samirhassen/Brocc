namespace nSavings.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountMaturityInformation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SavingsAccountHeader", "MaturesAt", c => c.DateTime());
            AddColumn("dbo.SavingsAccountHeader", "FixedInterestProduct", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SavingsAccountHeader", "FixedInterestProduct");
            DropColumn("dbo.SavingsAccountHeader", "MaturesAt");
        }
    }
}
