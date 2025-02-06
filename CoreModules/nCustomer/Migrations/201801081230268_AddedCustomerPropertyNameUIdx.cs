namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerPropertyNameUIdx : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE UNIQUE INDEX [CustomerPropertyNameUIdx] ON [dbo].[CustomerProperty]
(
	[CustomerId] ASC,
	[Name] ASC
)
WHERE ([IsCurrentData]=(1))");
        }

        public override void Down()
        {
            Sql(@"DROP INDEX [CustomerPropertyNameUIdx] ON [dbo].[CustomerProperty]");
        }
    }
}
