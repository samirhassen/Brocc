namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedInitialCreditCheckStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "InitialCreditCheckStatus", c => c.String(maxLength: 100));
            CreateIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", "InitialCreditCheckStatus");
        }

        public override void Down()
        {
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "InitialCreditCheckStatus" });
            DropColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "InitialCreditCheckStatus");
        }
    }
}
