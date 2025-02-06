namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAccountSubCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccountTransaction", "SubAccountCode", c => c.String(maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.AccountTransaction", "SubAccountCode");
        }
    }
}
