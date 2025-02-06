namespace nCreditReport.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerIdToCreditReport : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditReportHeader", "CustomerId", c => c.Int());
            CreateIndex("dbo.CreditReportHeader", "CustomerId");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditReportHeader", new[] { "CustomerId" });
            DropColumn("dbo.CreditReportHeader", "CustomerId");
        }
    }
}
