namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedProviderName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "ProviderName", c => c.String(maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.User", "ProviderName");
        }
    }
}
