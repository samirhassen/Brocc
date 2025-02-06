namespace nCreditReport.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class TryLaterDateAdded : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditReportHeader", "TryArchiveAfterDate", c => c.DateTimeOffset(precision: 7));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditReportHeader", "TryArchiveAfterDate");
        }
    }
}
