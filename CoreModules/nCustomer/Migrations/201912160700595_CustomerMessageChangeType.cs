namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CustomerMessageChangeType : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.CustomerMessage", "CreatedDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.CustomerMessage", "HandledDate", c => c.DateTime());
        }

        public override void Down()
        {
            AlterColumn("dbo.CustomerMessage", "HandledDate", c => c.DateTime(storeType: "date"));
            AlterColumn("dbo.CustomerMessage", "CreatedDate", c => c.DateTime(nullable: false, storeType: "date"));
        }
    }
}
