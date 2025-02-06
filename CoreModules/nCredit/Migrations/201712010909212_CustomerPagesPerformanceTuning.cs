namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CustomerPagesPerformanceTuning : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX CreditCustomerCustomerIdIdx ON [dbo].[CreditCustomer] ([CustomerId])");
        }

        public override void Down()
        {
            Sql("DROP INDEX CreditCustomerCustomerIdIdx ON [dbo].[CreditCustomer]");
        }
    }
}
