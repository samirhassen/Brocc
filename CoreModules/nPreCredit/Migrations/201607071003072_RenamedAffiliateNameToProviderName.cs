namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RenamedAffiliateNameToProviderName : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.CreditApplicationHeader", "AffiliateName", "ProviderName");
        }

        public override void Down()
        {
            RenameColumn("dbo.CreditApplicationHeader", "ProviderName", "AffiliateName");
        }
    }
}
