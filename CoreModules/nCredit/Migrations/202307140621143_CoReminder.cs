namespace nCredit.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CoReminder : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditReminderHeader", "IsCoReminderMaster", c => c.Boolean());
            AddColumn("dbo.CreditReminderHeader", "CoReminderId", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CreditReminderHeader", "CoReminderId");
            DropColumn("dbo.CreditReminderHeader", "IsCoReminderMaster");
        }
    }
}
