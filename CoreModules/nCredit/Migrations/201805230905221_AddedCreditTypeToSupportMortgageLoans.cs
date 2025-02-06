namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditTypeToSupportMortgageLoans : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditHeader", "CreditType", c => c.String(maxLength: 100, defaultValue: "UnsecuredLoan"));
            Sql("update dbo.CreditHeader set CreditType = 'UnsecuredLoan' where CreditType is null");
        }

        public override void Down()
        {
            DropColumn("dbo.CreditHeader", "CreditType");
        }
    }
}
