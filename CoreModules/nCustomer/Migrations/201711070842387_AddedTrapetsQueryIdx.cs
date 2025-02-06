namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTrapetsQueryIdx : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE NONCLUSTERED INDEX TrapetsQueryResultItemIdx2 ON [dbo].[TrapetsQueryResultItem] ([Name]) INCLUDE ([TrapetsQueryResultId],[Value])");
            Sql(@"CREATE NONCLUSTERED INDEX CustomerPropertyCustomerIdIdx ON [dbo].[CustomerProperty] ([CustomerId])");
        }

        public override void Down()
        {
            Sql(@"DROP INDEX TrapetsQueryResultItemIdx2 ON [dbo].[TrapetsQueryResultItem]");
            Sql(@"DROP INDEX CustomerPropertyCustomerIdIdx ON [dbo].[CustomerProperty]");
        }
    }
}
