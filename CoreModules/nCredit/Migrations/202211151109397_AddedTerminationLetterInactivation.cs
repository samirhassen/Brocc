namespace nCredit.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedTerminationLetterInactivation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditTerminationLetterHeader", "InactivatedByBusinessEventId", c => c.Int());
            AddColumn("dbo.CreditTerminationLetterHeader", "SuspendsCreditProcess", c => c.Boolean());
            CreateIndex("dbo.CreditTerminationLetterHeader", "InactivatedByBusinessEventId");
            AddForeignKey("dbo.CreditTerminationLetterHeader", "InactivatedByBusinessEventId", "dbo.BusinessEvent", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CreditTerminationLetterHeader", "InactivatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.CreditTerminationLetterHeader", new[] { "InactivatedByBusinessEventId" });
            DropColumn("dbo.CreditTerminationLetterHeader", "SuspendsCreditProcess");
            DropColumn("dbo.CreditTerminationLetterHeader", "InactivatedByBusinessEventId");
        }
    }
}
