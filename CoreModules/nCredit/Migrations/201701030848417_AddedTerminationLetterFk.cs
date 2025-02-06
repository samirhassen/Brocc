namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTerminationLetterFk : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CreditTerminationLetterHeader", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditTerminationLetterHeader", new[] { "CreditNr" });
            AlterColumn("dbo.CreditTerminationLetterHeader", "CreditNr", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.CreditTerminationLetterHeader", "CreditNr");
            AddForeignKey("dbo.CreditTerminationLetterHeader", "CreditNr", "dbo.CreditHeader", "CreditNr", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditTerminationLetterHeader", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditTerminationLetterHeader", new[] { "CreditNr" });
            AlterColumn("dbo.CreditTerminationLetterHeader", "CreditNr", c => c.String(maxLength: 128));
            CreateIndex("dbo.CreditTerminationLetterHeader", "CreditNr");
            AddForeignKey("dbo.CreditTerminationLetterHeader", "CreditNr", "dbo.CreditHeader", "CreditNr");
        }
    }
}
