namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedMortageLoanDocumentCheckStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "DocumentCheckStatus", c => c.String(maxLength: 100));
            CreateIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", "DocumentCheckStatus");
        }

        public override void Down()
        {
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "DocumentCheckStatus" });
            DropColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "DocumentCheckStatus");
        }
    }
}
