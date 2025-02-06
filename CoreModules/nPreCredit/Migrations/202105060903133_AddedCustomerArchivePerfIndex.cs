namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerArchivePerfIndex : DbMigration
    {
        public override void Up()
        {
            Sql("create index ArchiveCustomerPerfIdx1 ON [CreditApplicationHeader] ([Timestamp],[ArchivedDate])");
        }

        public override void Down()
        {
            Sql("drop index ArchiveCustomerPerfIdx1 ON [CreditApplicationHeader]");
        }
    }
}
