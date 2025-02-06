namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTimestampReplicationIdx1 : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX CustomerPropertyTsReplicationIdx1 ON [dbo].[CustomerProperty] ([IsCurrentData],[Name],[Timestamp]) INCLUDE ([CustomerId]) where IsCurrentData  = 1");
        }

        public override void Down()
        {
            Sql("DROP INDEX CustomerPropertyTsReplicationIdx1 ON [dbo].[CustomerProperty]");
        }
    }
}
