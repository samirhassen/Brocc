namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ConnectedCreditToItsCreationEvent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditHeader", "CreatedByBusinessEventId", c => c.Int(nullable: false));
            CreateIndex("dbo.CreditHeader", "CreatedByBusinessEventId");
            AddForeignKey("dbo.CreditHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent", "Id", cascadeDelete: false);
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.CreditHeader", new[] { "CreatedByBusinessEventId" });
            DropColumn("dbo.CreditHeader", "CreatedByBusinessEventId");
        }
    }
}
