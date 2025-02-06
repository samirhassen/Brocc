namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedInformationMetadata : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "InformationMetaData", c => c.String());
            AddColumn("dbo.EncryptedCreditApplicationItem", "InformationMetaData", c => c.String());
            AddColumn("dbo.CreditApplicationItem", "InformationMetaData", c => c.String());
            AddColumn("dbo.CreditApplicationSearchTerm", "InformationMetaData", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationSearchTerm", "InformationMetaData");
            DropColumn("dbo.CreditApplicationItem", "InformationMetaData");
            DropColumn("dbo.EncryptedCreditApplicationItem", "InformationMetaData");
            DropColumn("dbo.CreditApplicationHeader", "InformationMetaData");
        }
    }
}
