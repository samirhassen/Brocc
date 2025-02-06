namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class addTableManualSignature1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ManualSignature", "HandleByUser", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.ManualSignature", "HandleByUser");
        }
    }
}
