namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedExceptionData : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SystemLogItem", "ExceptionData", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.SystemLogItem", "ExceptionData");
        }
    }
}
