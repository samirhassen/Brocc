namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedDirectDebitCheckStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "DirectDebitCheckStatus", c => c.String(maxLength: 100));
            CreateIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", "DirectDebitCheckStatus");
        }

        public override void Down()
        {
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "DirectDebitCheckStatus" });
            DropColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "DirectDebitCheckStatus");
        }
    }
}
