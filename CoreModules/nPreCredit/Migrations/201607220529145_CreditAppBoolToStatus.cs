namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CreditAppBoolToStatus : DbMigration
    {
        public override void Up()
        {
            Sql("delete from dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationHeader", new[] { "CreditCheckStatus" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsCustomerCheckApproved" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsAgreementApproved" });
            AddColumn("dbo.CreditApplicationHeader", "CustomerCheckStatus", c => c.String(nullable: false, maxLength: 100));
            AddColumn("dbo.CreditApplicationHeader", "AgreementStatus", c => c.String(nullable: false, maxLength: 100));
            AddColumn("dbo.CreditApplicationHeader", "FraudCheckStatus", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.CreditApplicationHeader", "CreditCheckStatus", c => c.String(nullable: false, maxLength: 100));
            CreateIndex("dbo.CreditApplicationHeader", "CreditCheckStatus");
            CreateIndex("dbo.CreditApplicationHeader", "CustomerCheckStatus");
            CreateIndex("dbo.CreditApplicationHeader", "AgreementStatus");
            CreateIndex("dbo.CreditApplicationHeader", "FraudCheckStatus");
            DropColumn("dbo.CreditApplicationHeader", "IsCustomerCheckApproved");
            DropColumn("dbo.CreditApplicationHeader", "IsAgreementApproved");
        }

        public override void Down()
        {
            AddColumn("dbo.CreditApplicationHeader", "IsAgreementApproved", c => c.Boolean(nullable: false));
            AddColumn("dbo.CreditApplicationHeader", "IsCustomerCheckApproved", c => c.Boolean(nullable: false));
            DropIndex("dbo.CreditApplicationHeader", new[] { "FraudCheckStatus" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "AgreementStatus" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "CustomerCheckStatus" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "CreditCheckStatus" });
            AlterColumn("dbo.CreditApplicationHeader", "CreditCheckStatus", c => c.String(nullable: false, maxLength: 1));
            DropColumn("dbo.CreditApplicationHeader", "FraudCheckStatus");
            DropColumn("dbo.CreditApplicationHeader", "AgreementStatus");
            DropColumn("dbo.CreditApplicationHeader", "CustomerCheckStatus");
            CreateIndex("dbo.CreditApplicationHeader", "IsAgreementApproved");
            CreateIndex("dbo.CreditApplicationHeader", "IsCustomerCheckApproved");
            CreateIndex("dbo.CreditApplicationHeader", "CreditCheckStatus");
        }
    }
}
