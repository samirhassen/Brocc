namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAdditionalInfoFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "WaitingForAdditionalInformationDate", c => c.DateTimeOffset(precision: 7));
            CreateIndex("dbo.CreditApplicationHeader", "WaitingForAdditionalInformationDate");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "WaitingForAdditionalInformationDate" });
            DropColumn("dbo.CreditApplicationHeader", "WaitingForAdditionalInformationDate");
        }
    }
}
