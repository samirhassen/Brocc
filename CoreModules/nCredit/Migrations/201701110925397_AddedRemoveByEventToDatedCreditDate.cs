namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedRemoveByEventToDatedCreditDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DatedCreditDate", "RemovedByBusinessEventId", c => c.Int());
            CreateIndex("dbo.DatedCreditDate", "RemovedByBusinessEventId");
            AddForeignKey("dbo.DatedCreditDate", "RemovedByBusinessEventId", "dbo.BusinessEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.DatedCreditDate", "RemovedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.DatedCreditDate", new[] { "RemovedByBusinessEventId" });
            DropColumn("dbo.DatedCreditDate", "RemovedByBusinessEventId");
        }
    }
}
