namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CustomerMessageTextFormatAdded : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CustomerMessage", "TextFormat", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.CustomerMessage", "TextFormat");
        }
    }
}
