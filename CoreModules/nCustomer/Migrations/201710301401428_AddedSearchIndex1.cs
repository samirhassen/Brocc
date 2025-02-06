namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSearchIndex1 : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX CustomersWithSameDataSearchIdx1 ON [dbo].[CustomerProperty] ([IsCurrentData],[Name]) INCLUDE ([Value],[CustomerId])");
        }

        public override void Down()
        {
            Sql("DROP INDEX CustomersWithSameDataSearchIdx1 ON [dbo].[CustomerProperty]");
        }
    }
}
