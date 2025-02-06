namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedBusinessEventRoleCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LedgerAccountTransaction", "BusinessEventRoleCode", c => c.String(maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.LedgerAccountTransaction", "BusinessEventRoleCode");
        }
    }
}
