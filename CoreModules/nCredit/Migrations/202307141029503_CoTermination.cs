namespace nCredit.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CoTermination : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditTerminationLetterHeader", "IsCoTerminationMaster", c => c.Boolean());
            AddColumn("dbo.CreditTerminationLetterHeader", "CoTerminationId", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CreditTerminationLetterHeader", "CoTerminationId");
            DropColumn("dbo.CreditTerminationLetterHeader", "IsCoTerminationMaster");
        }
    }
}
