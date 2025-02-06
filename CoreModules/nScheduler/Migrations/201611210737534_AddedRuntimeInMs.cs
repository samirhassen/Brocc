namespace nScheduler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedRuntimeInMs : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ServiceRun", "RuntimeInMs", c => c.Long());
        }

        public override void Down()
        {
            DropColumn("dbo.ServiceRun", "RuntimeInMs");
        }
    }
}
