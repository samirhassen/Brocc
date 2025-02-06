namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAdditionalQuestionsStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "AdditionalQuestionsStatus", c => c.String(maxLength: 100));
            CreateIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", "AdditionalQuestionsStatus");
        }

        public override void Down()
        {
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "AdditionalQuestionsStatus" });
            DropColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "AdditionalQuestionsStatus");
        }
    }
}
