namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditNrToDocument : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditDocument", "CreditNr", c => c.String(maxLength: 128));
            CreateIndex("dbo.CreditDocument", "CreditNr");
            AddForeignKey("dbo.CreditDocument", "CreditNr", "dbo.CreditHeader", "CreditNr");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditDocument", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditDocument", new[] { "CreditNr" });
            DropColumn("dbo.CreditDocument", "CreditNr");
        }
    }
}
