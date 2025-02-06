namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedIsRejected : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "IsRejected", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.CreditApplicationHeader", "RejectedDate", c => c.DateTimeOffset(precision: 7));
            AddColumn("dbo.CreditApplicationHeader", "RejectedById", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationHeader", "RejectedById");
            DropColumn("dbo.CreditApplicationHeader", "RejectedDate");
            DropColumn("dbo.CreditApplicationHeader", "IsRejected");
        }
    }
}
