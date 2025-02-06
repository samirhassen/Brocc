namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedArchival : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "ArchivedDate", c => c.DateTimeOffset(precision: 7));
            AddColumn("dbo.CreditApplicationHeader", "ArchiveLevel", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationHeader", "ArchiveLevel");
            DropColumn("dbo.CreditApplicationHeader", "ArchivedDate");
        }
    }
}
