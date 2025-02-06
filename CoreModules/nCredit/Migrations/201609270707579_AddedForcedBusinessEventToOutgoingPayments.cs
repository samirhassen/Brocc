namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedForcedBusinessEventToOutgoingPayments : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OutgoingPaymentHeader", "CreatedByBusinessEventId", c => c.Int(nullable: false));
            CreateIndex("dbo.OutgoingPaymentHeader", "CreatedByBusinessEventId");
            AddForeignKey("dbo.OutgoingPaymentHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent", "Id", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("dbo.OutgoingPaymentHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.OutgoingPaymentHeader", new[] { "CreatedByBusinessEventId" });
            DropColumn("dbo.OutgoingPaymentHeader", "CreatedByBusinessEventId");
        }
    }
}
