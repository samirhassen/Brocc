namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemovedValidFromDate : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.DatedSavingsAccountString", "ValidFromDate");
            DropColumn("dbo.DatedSavingsAccountValue", "ValidFromDate");
            DropColumn("dbo.SharedDatedValue", "ValidFromDate");
        }

        public override void Down()
        {
            AddColumn("dbo.SharedDatedValue", "ValidFromDate", c => c.DateTime(nullable: false, storeType: "date"));
            AddColumn("dbo.DatedSavingsAccountValue", "ValidFromDate", c => c.DateTime(nullable: false, storeType: "date"));
            AddColumn("dbo.DatedSavingsAccountString", "ValidFromDate", c => c.DateTime(nullable: false, storeType: "date"));
        }
    }
}
