namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedHistoryEvent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationChangeLogItem", "EditEventId", c => c.Int());
            CreateIndex("dbo.CreditApplicationChangeLogItem", "EditEventId");
            AddForeignKey("dbo.CreditApplicationChangeLogItem", "EditEventId", "dbo.CreditApplicationEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationChangeLogItem", "EditEventId", "dbo.CreditApplicationEvent");
            DropIndex("dbo.CreditApplicationChangeLogItem", new[] { "EditEventId" });
            DropColumn("dbo.CreditApplicationChangeLogItem", "EditEventId");
        }
    }
}
