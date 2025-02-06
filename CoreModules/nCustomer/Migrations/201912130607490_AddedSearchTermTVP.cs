namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSearchTermTVP : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE TYPE [dbo].[NTechSearchTermTVP] AS TABLE(Value nvarchar(100) not null)");
            Sql("CREATE NONCLUSTERED INDEX [CustomerSearchTermIdx2] ON [dbo].[CustomerSearchTerm] ([TermCode],[IsActive]) INCLUDE ([CustomerId],[Value])");
        }

        public override void Down()
        {
            Sql("DROP TYPE [dbo].[NTechSearchTermTVP]");
            Sql("DROP INDEX [CustomerSearchTermIdx2] ON [dbo].[CustomerSearchTerm]");
        }
    }
}
