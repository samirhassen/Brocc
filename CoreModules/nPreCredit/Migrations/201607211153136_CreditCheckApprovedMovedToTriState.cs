namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CreditCheckApprovedMovedToTriState : DbMigration
    {
        public override void Up()
        {
            Sql("delete from dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsCreditCheckApproved" });
            AddColumn("dbo.CreditApplicationHeader", "CreditCheckStatus", c => c.String(nullable: false, maxLength: 1));
            CreateIndex("dbo.CreditApplicationHeader", "CreditCheckStatus");
            DropColumn("dbo.CreditApplicationHeader", "IsCreditCheckApproved");
        }

        public override void Down()
        {
            AddColumn("dbo.CreditApplicationHeader", "IsCreditCheckApproved", c => c.Boolean(nullable: false));
            DropIndex("dbo.CreditApplicationHeader", new[] { "CreditCheckStatus" });
            DropColumn("dbo.CreditApplicationHeader", "CreditCheckStatus");
            CreateIndex("dbo.CreditApplicationHeader", "IsCreditCheckApproved");
        }
    }
}
