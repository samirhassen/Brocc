namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedBusinessEventRoleCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccountTransaction", "BusinessEventRoleCode", c => c.String(maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.AccountTransaction", "BusinessEventRoleCode");
        }
    }
}
